using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradeEase.Models.Dtos;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ForexController : ApiControllerBase
{
    private readonly ILiveForexService _live;

    public ForexController(ILiveForexService live) => _live = live;

    [HttpGet]
    public ActionResult<ForexLiveResponseDto> List()
    {
        var snapshot = _live.GetSnapshot();
        return Ok(MapSnapshot(snapshot));
    }

    [HttpGet("trend/{currency}")]
    public ActionResult<IEnumerable<decimal>> Trend(string currency)
    {
        return Ok(_live.GetTrend(currency.ToUpperInvariant()));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ForexLiveResponseDto>> Refresh(CancellationToken ct)
    {
        await _live.RefreshAsync(ct);
        return Ok(MapSnapshot(_live.GetSnapshot()));
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public ActionResult<ForexLiveResponseDto> PublicRates()
    {
        var snapshot = _live.GetSnapshot();
        if (snapshot.Rates.Count == 0)
            return Ok(new ForexLiveResponseDto(Array.Empty<ForexRateDto>(), DateTime.UtcNow, snapshot.Source, 60));
        return Ok(MapSnapshot(snapshot));
    }

    private static ForexLiveResponseDto MapSnapshot(LiveForexSnapshot snapshot) =>
        new(
            snapshot.Rates.Select(r => new ForexRateDto(
                r.CurrencyCode,
                r.ExchangeRate,
                snapshot.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                r.ChangePct,
                r.IsUp)).ToList(),
            snapshot.UpdatedUtc,
            snapshot.Source,
            snapshot.RefreshIntervalSeconds);
}
