using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeEase.Models.Dtos;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class LiveController : ControllerBase
{
    private readonly ILiveDataSyncState _sync;
    private readonly ILiveForexService _forex;
    private readonly IWorldGeoService _geo;
    private readonly IWorldHsCatalogService _catalog;

    public LiveController(
        ILiveDataSyncState sync,
        ILiveForexService forex,
        IWorldGeoService geo,
        IWorldHsCatalogService catalog)
    {
        _sync = sync;
        _forex = forex;
        _geo = geo;
        _catalog = catalog;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public ActionResult<LiveStatusDto> Status()
    {
        var snapshot = _sync.GetSnapshot();
        var forex = _forex.GetSnapshot();
        var feeds = snapshot.Feeds.Select(f => new LiveFeedStatusDto(
            f.Name, f.LastSuccessUtc, f.Source, f.ItemCount, f.RefreshIntervalSeconds, f.LastError)).ToList();

        return Ok(new LiveStatusDto(
            snapshot.ServerUtc,
            feeds,
            forex.UpdatedUtc,
            forex.Source,
            forex.Rates.Count,
            _geo.LastRefreshUtc,
            _geo.Source,
            _geo.GetCountries().Count,
            _catalog.LastRefreshUtc,
            _catalog.CatalogSource,
            _catalog.TotalProducts));
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LiveStatusDto>> ForceRefresh(CancellationToken ct)
    {
        await _forex.RefreshAsync(ct);
        await _geo.RefreshFromApiAsync(ct);
        await _catalog.RefreshFromExternalApisAsync(ct);
        return Status();
    }
}
