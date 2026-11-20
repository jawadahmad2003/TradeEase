using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SchemesController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly ILiveForexService _forex;

    public SchemesController(TradeEaseDbContext db, ILiveForexService forex)
    {
        _db = db;
        _forex = forex;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchemeDto>>> List()
    {
        var schemes = await _db.ExportSchemes.OrderBy(s => s.SchemeCode).ToListAsync();
        return Ok(schemes.Select(MapScheme));
    }

    [HttpPost("check")]
    public async Task<ActionResult<SchemeCheckResponse>> Check([FromBody] SchemeCheckRequest request)
    {
        var schemes = await _db.ExportSchemes.Where(s => s.IsActive).ToListAsync();
        var eligible = EvaluateEligibility(schemes, request).ToList();

        var usdRate = _forex.GetUsdToPkr();

        var savings = eligible.Count > 0
            ? request.TurnoverUsd * usdRate * 0.08m
            : 0m;

        var userId = User.GetUserId();
        foreach (var scheme in eligible)
        {
            _db.SchemeEligibilityResults.Add(new SchemeEligibilityResult
            {
                UserId = userId,
                SchemeId = scheme.SchemeId,
                IsEligible = true,
                CheckedAt = DateTime.UtcNow,
            });
        }
        if (eligible.Count > 0)
            await _db.SaveChangesAsync();

        return Ok(new SchemeCheckResponse(
            eligible.Select(s => new SchemeCheckResultDto(
                s.SchemeCode, s.SchemeName, s.Agency, s.BenefitDetails,
                s.ApplicationDeadline, true)).ToList(),
            savings));
    }

    private static IEnumerable<ExportScheme> EvaluateEligibility(
        IEnumerable<ExportScheme> schemes, SchemeCheckRequest request)
    {
        var isExporter = !string.IsNullOrWhiteSpace(request.TraderType)
            && request.TraderType != "First-time Importer Only";
        var fbrOk = request.FbrStatus == "Active Filer";
        if (!isExporter || !fbrOk) yield break;

        foreach (var scheme in schemes)
        {
            var match = scheme.SchemeCode switch
            {
                "EFS" => request.ExportPct >= 30 && request.TurnoverUsd >= 50000,
                "DDT" => request.ExportPct >= 20,
                "SBP-EFS" => request.TurnoverUsd >= 100000,
                "EMDF" => request.ExportPct >= 10,
                "LTFF" => request.Category != "Agriculture Only",
                _ => request.ExportPct >= (int)scheme.MinExportPct
                    && request.TurnoverUsd >= (int)scheme.MinTurnoverUsd,
            };
            if (match) yield return scheme;
        }
    }

    private static SchemeDto MapScheme(ExportScheme s) => new(
        s.SchemeId, s.SchemeCode, s.SchemeName, s.Agency, s.BenefitDetails,
        s.ApplicationDeadline, s.MinExportPct, s.MinTurnoverUsd, s.IsActive);
}
