namespace TradeEase.Services;

public class ForexBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILiveDataSyncState _sync;
    private readonly ILogger<ForexBackgroundService> _logger;
    private readonly IConfiguration _config;

    public ForexBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILiveDataSyncState sync,
        ILogger<ForexBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _sync = sync;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int?>("ForexApi:RefreshIntervalMinutes");
        if (!intervalMinutes.HasValue || intervalMinutes.Value <= 0)
        {
            var intervalHours = _config.GetValue<int?>("ForexApi:RefreshIntervalHours") ?? 0;
            intervalMinutes = Math.Max(1, intervalHours * 60);
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes.Value));

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var live = scope.ServiceProvider.GetRequiredService<ILiveForexService>();
                await live.RefreshAsync(stoppingToken);
                var snapshot = live.GetSnapshot();
                _sync.MarkForexRefreshed(snapshot.Source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled live forex refresh failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
