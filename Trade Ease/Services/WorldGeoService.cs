namespace TradeEase.Services;

using TradeEase.Models.Dtos;

public interface IWorldGeoService
{
    IReadOnlyList<CountryDetailDto> GetCountries();
    IReadOnlyList<PortDetailDto> GetPorts(string countryCode);
    CountryDetailDto? GetCountry(string countryCode);
    DateTime? LastRefreshUtc { get; }
    string Source { get; }
    Task RefreshFromApiAsync(CancellationToken ct = default);
}

public class WorldGeoService : IWorldGeoService
{
    private readonly IRestCountriesGeoClient _restCountries;
    private readonly ILiveDataSyncState _sync;
    private readonly ILogger<WorldGeoService> _logger;
    private readonly object _lock = new();
    private IReadOnlyList<CountryDetailDto> _countries;
    private DateTime? _lastRefreshUtc;
    private string _source = "embedded fallback";

    public WorldGeoService(
        IRestCountriesGeoClient restCountries,
        ILiveDataSyncState sync,
        ILogger<WorldGeoService> logger)
    {
        _restCountries = restCountries;
        _sync = sync;
        _logger = logger;
        _countries = WorldGeoData.BuildAll();
    }

    public DateTime? LastRefreshUtc { get { lock (_lock) return _lastRefreshUtc; } }
    public string Source { get { lock (_lock) return _source; } }

    public IReadOnlyList<CountryDetailDto> GetCountries()
    {
        lock (_lock) return _countries;
    }

    public CountryDetailDto? GetCountry(string countryCode)
    {
        lock (_lock)
            return _countries.FirstOrDefault(c => c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<PortDetailDto> GetPorts(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return Array.Empty<PortDetailDto>();

        var country = GetCountry(countryCode);
        var name = country?.Name ?? WorldGeoData.TryGetCountryName(countryCode) ?? countryCode.ToUpperInvariant();
        return WorldGeoData.GetPortsForCode(countryCode, name);
    }

    public async Task RefreshFromApiAsync(CancellationToken ct = default)
    {
        var fetched = await _restCountries.FetchAllAsync(ct);
        if (fetched.Count == 0)
        {
            _sync.MarkGeoError("REST Countries API unavailable — using embedded list.");
            return;
        }

        var updated = fetched
            .Select(c => new CountryDetailDto(
                c.Code,
                c.Name,
                c.Region,
                Array.Empty<PortDetailDto>()))
            .ToList();

        lock (_lock)
        {
            _countries = updated;
            _lastRefreshUtc = DateTime.UtcNow;
            _source = "restcountries.com (live) + TradeEase port registry";
        }

        _sync.MarkGeoRefreshed(_source, updated.Count);
        _logger.LogInformation("Geo data refreshed from API: {Count} countries", updated.Count);
    }
}
