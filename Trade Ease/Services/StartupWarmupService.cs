namespace TradeEase.Services;

using Microsoft.EntityFrameworkCore;

/// <summary>Loads live data in the background so Kestrel starts immediately.</summary>
public class StartupWarmupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<StartupWarmupService> _logger;

    public StartupWarmupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<StartupWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("LiveData:BackgroundWarmup", true))
            return;

        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TradeEase.Data.TradeEaseDbContext>();
            var sync = scope.ServiceProvider.GetRequiredService<ILiveDataSyncState>();

            if (!await db.Database.CanConnectAsync(stoppingToken))
            {
                _logger.LogWarning("Database not ready — skipping background warmup.");
                return;
            }

            var forex = scope.ServiceProvider.GetRequiredService<ILiveForexService>();
            try
            {
                await forex.RefreshAsync(stoppingToken);
                sync.MarkForexRefreshed(forex.GetSnapshot().Source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background forex warmup skipped.");
            }

            if (_config.GetValue("LiveData:RefreshGeoOnStartup", false))
            {
                var geo = scope.ServiceProvider.GetRequiredService<IWorldGeoService>();
                try
                {
                    await geo.RefreshFromApiAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background geo warmup skipped.");
                }
            }

            if (_config.GetValue("LiveData:SyncEmbeddedOnStartup", true))
            {
                var embeddedCount = scope.ServiceProvider.GetRequiredService<IWorldHsCatalogService>().TotalProducts;
                var dbCount = await db.HSCodes.CountAsync(stoppingToken);
                if (dbCount < embeddedCount)
                {
                    var catalog = scope.ServiceProvider.GetRequiredService<IWorldHsCatalogService>();
                    try
                    {
                        await catalog.SyncToDatabaseAsync(db, stoppingToken);
                        _logger.LogInformation("Embedded HS catalog synced to database ({Count} seed rows).", embeddedCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background HS catalog sync skipped.");
                    }
                }
            }

            _logger.LogInformation("TradeEase background warmup complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background warmup failed.");
        }
    }
}
