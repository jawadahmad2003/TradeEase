using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TradeEase.Services;

public interface IRestCountriesGeoClient
{
    Task<IReadOnlyList<(string Code, string Name, string Region, string? Capital)>> FetchAllAsync(CancellationToken ct = default);
}

public class RestCountriesGeoClient : IRestCountriesGeoClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RestCountriesGeoClient> _logger;

    public RestCountriesGeoClient(IHttpClientFactory httpClientFactory, ILogger<RestCountriesGeoClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(string Code, string Name, string Region, string? Capital)>> FetchAllAsync(
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("RestCountries");
        var url = "v3.1/all?fields=name,cca2,region,subregion,capital";

        try
        {
            var rows = await client.GetFromJsonAsync<List<RestCountryRow>>(url, ct) ?? new List<RestCountryRow>();
            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Cca2) && r.Name?.Common is not null)
                .Select(r => new
                {
                    Code = r.Cca2!.ToUpperInvariant(),
                    Name = r.Name!.Common!,
                    Region = string.IsNullOrWhiteSpace(r.Subregion) ? (r.Region ?? "Other") : r.Subregion!,
                    Capital = r.Capital?.FirstOrDefault(),
                })
                .OrderBy(r => r.Name)
                .Select(r => (r.Code, r.Name, r.Region, r.Capital))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST Countries API fetch failed");
            return Array.Empty<(string Code, string Name, string Region, string? Capital)>();
        }
    }

    private sealed class RestCountryRow
    {
        [JsonPropertyName("name")]
        public RestCountryName? Name { get; set; }

        [JsonPropertyName("cca2")]
        public string? Cca2 { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("subregion")]
        public string? Subregion { get; set; }

        [JsonPropertyName("capital")]
        public List<string>? Capital { get; set; }
    }

    private sealed class RestCountryName
    {
        [JsonPropertyName("common")]
        public string? Common { get; set; }
    }
}
