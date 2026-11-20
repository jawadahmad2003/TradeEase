namespace TradeEase.Services;

/// <summary>Legacy interface — refreshes in-memory live rates only (no database writes).</summary>
public interface IForexRefreshService
{
    Task<IReadOnlyList<LiveForexRate>> RefreshRatesAsync(CancellationToken cancellationToken = default);
}

public class ForexRefreshService : IForexRefreshService
{
    private readonly ILiveForexService _live;

    public ForexRefreshService(ILiveForexService live) => _live = live;

    public async Task<IReadOnlyList<LiveForexRate>> RefreshRatesAsync(CancellationToken cancellationToken = default)
    {
        await _live.RefreshAsync(cancellationToken);
        return _live.GetSnapshot().Rates;
    }
}
