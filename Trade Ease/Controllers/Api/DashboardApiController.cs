using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;

using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly ILiveForexService _forex;

    public DashboardController(TradeEaseDbContext db, ILiveForexService forex)
    {
        _db = db;
        _forex = forex;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] int? days, [FromQuery] string? type)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var query = _db.Shipments.AsNoTracking().AsQueryable();
        if (!isAdmin)
            query = query.Where(s => s.UserId == userId);

        if (days.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(s => s.CreatedAt >= cutoff);
        }

        if (!string.IsNullOrEmpty(type) && type != "All Types")
        {
            query = query.Where(s => s.ShipmentType == type);
        }

        var total = await query.CountAsync();
        var pending = await query.CountAsync(s => s.ShipmentStatus != "Cleared");
        var cleared = await query.CountAsync(s => s.ShipmentStatus == "Cleared");
        var imports = await query.CountAsync(s => s.ShipmentType == "Import");
        var exports = await query.CountAsync(s => s.ShipmentType == "Export");
        // SumAsync on nullable decimal returns null when set is empty — coalesce to 0
        var totalTrade = await query.SumAsync(s => s.TotalValuePKR) ?? 0;

        // Project raw DB scalar fields first; call C# helper AFTER materializing
        // (PakistanTradeReference.FormatRoute cannot be translated to SQL)
        var recentRaw = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new
            {
                s.ShipmentId,
                s.ShipmentReference,
                s.ProductDesc,
                s.ShipmentStatus,
                s.TotalValuePKR,
                s.CreatedAt,
                s.ShipmentType,
                s.OriginCountry,
                s.DestinationCountry,
            })
            .ToListAsync();

        var recent = recentRaw
            .Select(s => new ShipmentSummaryDto(
                s.ShipmentId,
                s.ShipmentReference,
                s.ProductDesc,
                s.ShipmentStatus,
                s.TotalValuePKR,
                s.CreatedAt,
                s.ShipmentType,
                PakistanTradeReference.FormatRoute(s.OriginCountry, s.DestinationCountry)))
            .ToList();

        // Materialize the join rows first (EF Core cannot translate GroupBy after a cross-DbSet Join)
        var partnerRows = await query
            .Where(s => s.PartyId.HasValue)
            .Join(_db.TradeParties.AsNoTracking(),
                s => s.PartyId,
                p => p.PartyId,
                (s, p) => new { s.PartyId, p.Name, p.PartyType, p.Country, s.TotalValuePKR })
            .ToListAsync();

        var topPartners = partnerRows
            .GroupBy(x => new { x.PartyId, x.Name, x.PartyType, x.Country })
            .Select(g => new TopTradePartnerDto(
                g.Key.PartyId ?? 0,
                g.Key.Name,
                g.Key.PartyType,
                g.Key.Country,
                g.Count(),
                g.Sum(x => x.TotalValuePKR)))
            .OrderByDescending(x => x.ShipmentCount)
            .ThenByDescending(x => x.TradeValuePkr ?? 0)
            .Take(5)
            .ToList();

        var snapshot = _forex.GetSnapshot();
        var priority = PakistanTradeReference.PriorityForexCodes;
        var forex = snapshot.Rates
            .OrderBy(r =>
            {
                var idx = Array.IndexOf(priority, r.CurrencyCode);
                return idx >= 0 ? idx : 100;
            })
            .ThenBy(r => r.CurrencyCode)
            .Select(r => new ForexRateDto(
                r.CurrencyCode,
                r.ExchangeRate,
                snapshot.UpdatedUtc.ToString("yyyy-MM-dd HH:mm") + " UTC",
                r.ChangePct,
                r.IsUp))
            .ToList();

        return Ok(new DashboardDto(
            total,
            pending,
            cleared,
            totalTrade,
            recent,
            forex,
            topPartners,
            imports,
            exports));
    }
}
