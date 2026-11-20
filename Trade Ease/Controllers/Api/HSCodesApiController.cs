using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class HSCodesController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly IWorldHsCatalogService _catalog;
    private readonly ILiveTradeTariffService _tariff;

    public HSCodesController(
        TradeEaseDbContext db,
        IWorldHsCatalogService catalog,
        ILiveTradeTariffService tariff)
    {
        _db = db;
        _catalog = catalog;
        _tariff = tariff;
    }

    [HttpGet("products")]
    public async Task<ActionResult<ProductSearchResponse>> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int limit = 50,
        [FromQuery] string? shipmentType = null,
        [FromQuery] string? originCountry = null,
        [FromQuery] string? destinationCountry = null,
        CancellationToken ct = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        var route = TradeRouteContext.Normalize(shipmentType, originCountry, destinationCountry);

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(new ProductSearchResponse(
                _catalog.TotalProducts,
                Array.Empty<HSCodeDto>(),
                "FY 2025-26",
                DateTime.UtcNow,
                "Type at least 2 characters to search live harmonized products",
                false,
                route.RouteLabel,
                route.IsImportToPakistan ? "FBR import · any origin → PK" : $"Export · PK → {route.DestinationCountry}",
                route.ShipmentType,
                route.OriginCountry,
                route.DestinationCountry));
        }

        var unified = await UnifiedSearchAsync(q, take, route, ct);
        var enriched = unified.Select(item => _tariff.MapProductFast(item, route)).ToList();
        var liveMerged = enriched.Any(i => i.LiveApi);

        return Ok(new ProductSearchResponse(
            _catalog.TotalProducts,
            enriched,
            "FY 2025-26",
            DateTime.UtcNow,
            liveMerged
                ? $"Live harmonized search + route tariff ({route.RouteLabel})"
                : $"Route tariff applied · {route.RouteLabel}",
            liveMerged,
            route.RouteLabel,
            route.IsImportToPakistan
                ? "FBR import · any origin → PK"
                : $"Export · PK → any destination ({route.DestinationCountry})",
            route.ShipmentType,
            route.OriginCountry,
            route.DestinationCountry));
    }

    [HttpGet("resolve")]
    public async Task<ActionResult<HSCodeDto>> ResolveProduct(
        [FromQuery] string hsCode,
        [FromQuery] string? shipmentType = null,
        [FromQuery] string? originCountry = null,
        [FromQuery] string? destinationCountry = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hsCode))
            return BadRequest(new { message = "HS code is required." });

        var route = TradeRouteContext.Normalize(shipmentType, originCountry, destinationCountry);
        try
        {
            return Ok(await _tariff.ResolveByCodeAsync(hsCode.Trim(), route, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<HSCodeSearchResponse>> Search([FromQuery] string? q, [FromQuery] int limit = 50)
    {
        var take = Math.Clamp(limit, 1, 100);
        var route = TradeRouteContext.Normalize("Import", "CN", "PK");

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            var total = await _db.HSCodes.CountAsync(h => h.IsActive);
            return Ok(new HSCodeSearchResponse(
                Math.Max(total, _catalog.TotalProducts),
                Array.Empty<HSCodeDto>(),
                "FY 2025-26",
                DateTime.UtcNow));
        }

        var unified = await UnifiedSearchAsync(q, take, route, HttpContext.RequestAborted);
        var enriched = unified.Select(item => _tariff.MapProductFast(item, route)).ToList();

        return Ok(new HSCodeSearchResponse(
            _catalog.TotalProducts,
            enriched,
            "FY 2025-26",
            DateTime.UtcNow));
    }

    private async Task<IReadOnlyList<ProductSearchResult>> UnifiedSearchAsync(string q, int take, TradeRouteContext route, CancellationToken ct)
    {
        var merged = new Dictionary<string, ProductSearchResult>(StringComparer.OrdinalIgnoreCase);

        var term = q.Trim();
        var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var dbQuery = _db.HSCodes.AsNoTracking().Where(h => h.IsActive);
        dbQuery = dbQuery.Where(h => h.Code.Contains(term) || h.Description.Contains(term));
        
        var dbHits = await dbQuery.Take(100).ToListAsync(ct);
        foreach (var h in dbHits)
        {
            merged[h.Code] = new ProductSearchResult(
                h.Code,
                h.Description,
                h.Chapter ?? (h.Code.Length >= 2 ? h.Code[..2] : "00"),
                h.UnitOfMeasure ?? "KG",
                h.CustomsDutyPct,
                h.SalesTaxPct,
                h.AITPct,
                h.TariffYear,
                TradeProductHash.ComputeForRoute(h.Code, h.Description, route),
                false);
        }

        var catalogHits = await _catalog.SearchLiveAsync(q, take, ct);
        foreach (var hit in catalogHits)
        {
            if (!merged.ContainsKey(hit.Code))
            {
                merged[hit.Code] = hit;
            }
        }

        return merged.Values
            .Select(p => new { Product = p, Score = ScoreResult(p.Code, p.Description, term, tokens) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Code)
            .Select(x => x.Product)
            .Take(take)
            .ToList();
    }

    private static int ScoreResult(string code, string description, string term, string[] tokens)
    {
        var score = 0;
        if (code.StartsWith(term, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (code.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 60;
        if (description.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 40;

        foreach (var token in tokens)
        {
            if (code.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 20;
            if (description.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 15;
        }

        return score;
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> Count(CancellationToken ct)
    {
        var dbTotal = await _db.HSCodes.CountAsync(h => h.IsActive, ct);
        return Ok(new
        {
            total = Math.Max(dbTotal, _catalog.TotalProducts),
            tariffYear = "FY 2025-26",
            source = "embedded + live search on demand",
        });
    }

    [HttpPost("calculate-duty")]
    public async Task<ActionResult<DutyCalcResponse>> CalculateDuty([FromBody] DutyCalcRequest request, CancellationToken ct = default)
    {
        var route = TradeRouteContext.Normalize(request.ShipmentType, request.OriginCountry, request.DestinationCountry);

        var hs = await _db.HSCodes.AsNoTracking().FirstOrDefaultAsync(h => h.Code == request.HsCode && h.IsActive, ct);
        if (hs is null)
        {
            var catalog = _catalog.GetByCode(request.HsCode);
            if (catalog is null)
            {
                try
                {
                    var resolved = await _tariff.ResolveByCodeAsync(request.HsCode, route, ct);
                    hs = new HSCode
                    {
                        Code = resolved.Code,
                        Description = resolved.Description,
                        Chapter = resolved.Code.Length >= 2 ? resolved.Code[..2] : "00",
                        UnitOfMeasure = resolved.UnitOfMeasure,
                        CustomsDutyPct = resolved.CustomsDutyPct,
                        SalesTaxPct = resolved.SalesTaxPct,
                        AITPct = resolved.AITPct,
                        TariffYear = resolved.TariffYear,
                        IsActive = true,
                    };
                }
                catch
                {
                    return NotFound(new { message = "HS code not found." });
                }
            }
            else
            {
                hs = new HSCode
                {
                    Code = catalog.Code,
                    Description = catalog.Description,
                    Chapter = catalog.Chapter,
                    UnitOfMeasure = catalog.Unit,
                    CustomsDutyPct = catalog.CustomsDutyPct,
                    SalesTaxPct = catalog.SalesTaxPct,
                    AITPct = catalog.AITPct,
                    TariffYear = catalog.TariffYear,
                    IsActive = true,
                };
            }

            _db.HSCodes.Add(hs);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(await _tariff.CalculateForRouteAsync(hs, request.InvoiceValue, request.Currency, route, ct));
    }
}
