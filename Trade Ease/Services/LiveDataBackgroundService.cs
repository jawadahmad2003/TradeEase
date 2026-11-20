namespace TradeEase.Services;

public class LiveDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILiveDataSyncState _sync;
    private readonly ILogger<LiveDataBackgroundService> _logger;
    private readonly IConfiguration _config;

    public LiveDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILiveDataSyncState sync,
        ILogger<LiveDataBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _sync = sync;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        var geoHours = _config.GetValue("LiveData:GeoRefreshHours", 24);
        var catalogHours = _config.GetValue("LiveData:CatalogRefreshHours", 24);
        var geoInterval = TimeSpan.FromHours(Math.Max(6, geoHours));
        var catalogInterval = TimeSpan.FromHours(Math.Max(12, catalogHours));

        var nextGeo = DateTime.UtcNow.AddMinutes(10);
        var nextCatalog = DateTime.UtcNow.AddHours(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var geo = scope.ServiceProvider.GetRequiredService<IWorldGeoService>();
                var catalog = scope.ServiceProvider.GetRequiredService<IWorldHsCatalogService>();
                var db = scope.ServiceProvider.GetRequiredService<TradeEase.Data.TradeEaseDbContext>();

                if (DateTime.UtcNow >= nextGeo)
                {
                    await geo.RefreshFromApiAsync(stoppingToken);
                    nextGeo = DateTime.UtcNow.Add(geoInterval);
                }

                if (DateTime.UtcNow >= nextCatalog)
                {
                    var refreshAllChapters = _config.GetValue("LiveData:RefreshAllHsChapters", false);
                    if (refreshAllChapters)
                    {
                        await catalog.RefreshFromExternalApisAsync(stoppingToken);
                        if (await db.Database.CanConnectAsync(stoppingToken))
                            await catalog.SyncToDatabaseAsync(db, stoppingToken);
                    }

                    nextCatalog = DateTime.UtcNow.Add(catalogInterval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Live data background refresh failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
