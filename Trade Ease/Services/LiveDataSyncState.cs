namespace TradeEase.Services;

public interface ILiveDataSyncState
{
    LiveSyncSnapshot GetSnapshot();
    void MarkForexRefreshed(string source);
    void MarkGeoRefreshed(string source, int countryCount);
    void MarkCatalogRefreshed(string source, int productCount);
    void MarkCatalogSearch(string source);
    void MarkGeoError(string error);
}

public record LiveFeedStatus(
    string Name,
    DateTime? LastSuccessUtc,
    string Source,
    int? ItemCount,
    int RefreshIntervalSeconds,
    string? LastError);

public record LiveSyncSnapshot(
    DateTime ServerUtc,
    IReadOnlyList<LiveFeedStatus> Feeds);

public class LiveDataSyncState : ILiveDataSyncState
{
    private readonly object _lock = new();
    private DateTime? _forexUtc;
    private string _forexSource = "pending";
    private DateTime? _geoUtc;
    private string _geoSource = "pending";
    private int _geoCount;
    private string? _geoError;
    private DateTime? _catalogUtc;
    private string _catalogSource = "pending";
    private int _catalogCount;
    private string? _catalogError;
    private DateTime? _lastSearchUtc;
    private string _lastSearchSource = "";

    public LiveSyncSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new LiveSyncSnapshot(
                DateTime.UtcNow,
                new LiveFeedStatus[]
                {
                    new("forex", _forexUtc, _forexSource, null, 60, null),
                    new("countries", _geoUtc, _geoSource, _geoCount, 6 * 3600, _geoError),
                    new("products", _catalogUtc, _catalogSource, _catalogCount, 6 * 3600, _catalogError),
                    new("product_search", _lastSearchUtc, string.IsNullOrEmpty(_lastSearchSource) ? "on-demand" : _lastSearchSource, null, 0, null),
                });
        }
    }

    public void MarkForexRefreshed(string source)
    {
        lock (_lock)
        {
            _forexUtc = DateTime.UtcNow;
            _forexSource = source;
        }
    }

    public void MarkGeoRefreshed(string source, int countryCount)
    {
        lock (_lock)
        {
            _geoUtc = DateTime.UtcNow;
            _geoSource = source;
            _geoCount = countryCount;
            _geoError = null;
        }
    }

    public void MarkCatalogRefreshed(string source, int productCount)
    {
        lock (_lock)
        {
            _catalogUtc = DateTime.UtcNow;
            _catalogSource = source;
            _catalogCount = productCount;
            _catalogError = null;
        }
    }

    public void MarkCatalogSearch(string source)
    {
        lock (_lock)
        {
            _lastSearchUtc = DateTime.UtcNow;
            _lastSearchSource = source;
        }
    }

    public void MarkGeoError(string error)
    {
        lock (_lock) { _geoError = error; }
    }

    public void MarkCatalogError(string error)
    {
        lock (_lock) { _catalogError = error; }
    }
}
