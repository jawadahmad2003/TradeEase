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
public class ShipmentsController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly IDutyCalculatorService _duty;
    private readonly ILiveForexService _forex;
    private readonly IWorldHsCatalogService _catalog;

    public ShipmentsController(TradeEaseDbContext db, IDutyCalculatorService duty, ILiveForexService forex, IWorldHsCatalogService catalog)
    {
        _db = db;
        _duty = duty;
        _forex = forex;
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShipmentListDto>>> List()
    {
        var query = _db.Shipments
            .AsNoTracking()
            .Include(s => s.Party)
            .Include(s => s.HSCode)
            .AsQueryable();

        if (!User.IsAdmin())
            query = query.Where(s => s.UserId == User.GetUserId());

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return Ok(list.Select(MapList));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShipmentDetailDto>> Get(int id)
    {
        var shipment = await _db.Shipments
            .AsNoTracking()
            .Include(s => s.Party)
            .Include(s => s.HSCode)
            .Include(s => s.StatusHistory)
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.ShipmentId == id);

        if (shipment is null) return NotFound();
        if (!User.IsAdmin() && shipment.UserId != User.GetUserId()) return Forbid();

        return Ok(new ShipmentDetailDto(
            shipment.ShipmentId,
            shipment.ShipmentReference,
            shipment.ShipmentType,
            shipment.ProductDesc,
            shipment.ShipmentStatus,
            shipment.InvoiceValue,
            shipment.Currency,
            shipment.TotalValuePKR,
            shipment.OriginPort,
            shipment.DestinationPort,
            shipment.DestinationCountry,
            shipment.HSCode?.Code,
            shipment.Party?.Name,
            shipment.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new StatusHistoryDto(h.Status, h.ChangedAt, h.Remarks))
                .ToList(),
            shipment.Documents
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new ShipmentDocumentDto(
                    d.DocumentId, d.DocumentType, d.DocumentName, d.Status, d.UploadedAt))
                .ToList()));
    }

    [HttpPost]
    [Authorize(Roles = "Trader")]
    public async Task<ActionResult<ShipmentListDto>> Create([FromBody] CreateShipmentRequest request)
    {
        var hs = await _db.HSCodes.FirstOrDefaultAsync(h => h.Code == request.HsCode.Trim() && h.IsActive);
        if (hs is null)
        {
            var catalog = _catalog.GetByCode(request.HsCode.Trim());
            if (catalog is null)
                return BadRequest(new { message = "Product/HS code not found. Search and select from the catalog." });

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
            _db.HSCodes.Add(hs);
            await _db.SaveChangesAsync();
        }

        if (request.PartyId.HasValue)
        {
            var party = await _db.TradeParties.FindAsync(request.PartyId.Value);
            if (party is null || party.UserId != User.GetUserId())
                return BadRequest(new { message = "Invalid trade party." });
        }

        var usdRate = _forex.GetUsdToPkr();

        decimal valueUsd;
        if (request.Currency == "USD")
            valueUsd = request.InvoiceValue;
        else if (request.Currency == "PKR")
            valueUsd = request.InvoiceValue / usdRate;
        else
        {
            var curRate = _forex.GetCurrencyToPkr(request.Currency, usdRate);
            if (curRate <= 0)
                return BadRequest(new { message = "Live forex rate not available for currency." });
            valueUsd = (request.InvoiceValue * curRate) / usdRate;
        }

        var breakdown = _duty.Calculate(valueUsd, hs, usdRate);
        var year = DateTime.UtcNow.Year;
        var seq = await _db.Shipments.CountAsync(s => s.CreatedAt.Year == year) + 1;
        var reference = $"SHP-{year}-{seq:D4}";

        var shipment = new Shipment
        {
            ShipmentReference = reference,
            UserId = User.GetUserId(),
            PartyId = request.PartyId,
            HSCodeId = hs.HSCodeId,
            ShipmentType = request.ShipmentType,
            ProductDesc = request.ProductDesc.Trim(),
            InvoiceValue = request.InvoiceValue,
            Currency = request.Currency,
            OriginPort = request.OriginPort.Trim(),
            DestinationPort = request.DestinationPort.Trim(),
            DestinationCountry = request.DestinationCountry.Trim(),
            OriginCountry = request.OriginCountry.Trim(),
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure,
            TotalValuePKR = breakdown.AssessedValuePkr,
            ShipmentStatus = "Booked",
            ShipmentDate = request.ShipmentDate,
            ClearanceDate = request.ClearanceDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        _db.ShipmentStatusHistory.Add(new ShipmentStatusHistory
        {
            ShipmentId = shipment.ShipmentId,
            Status = "Booked",
            Remarks = "Shipment created",
            ChangedBy = User.GetUserId(),
            ChangedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        shipment.HSCode = hs;
        return Ok(MapList(shipment));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var status = request.Status;
        var shipment = await _db.Shipments.FindAsync(id);
        if (shipment is null) return NotFound();
        if (!User.IsAdmin() && shipment.UserId != User.GetUserId()) return Forbid();

        shipment.ShipmentStatus = status;
        _db.ShipmentStatusHistory.Add(new ShipmentStatusHistory
        {
            ShipmentId = id,
            Status = status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = User.GetUserId(),
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ShipmentListDto MapList(Shipment s)
    {
        var route = $"{s.OriginPort ?? "—"} → {s.DestinationPort ?? s.DestinationCountry ?? "—"}";
        var value = s.TotalValuePKR.HasValue
            ? $"PKR {s.TotalValuePKR.Value:N2}"
            : $"{s.Currency} {s.InvoiceValue:N2}";

        return new ShipmentListDto(
            s.ShipmentId,
            s.ShipmentReference,
            s.ShipmentType,
            s.HSCode?.Code,
            s.ProductDesc,
            null,
            route,
            s.ShipmentDate?.ToString("dd-MMM-yyyy"),
            s.ClearanceDate?.ToString("dd-MMM-yyyy"),
            value,
            s.ShipmentStatus,
            s.CreatedAt);
    }

    [HttpGet("draft")]
    public ActionResult GetDraft()
    {
        var draft = HttpContext.Session.GetString("ShipmentDraft");
        if (string.IsNullOrEmpty(draft)) return NotFound();
        return Content(draft, "application/json");
    }

    [HttpPost("draft")]
    public ActionResult SaveDraft([FromBody] object draft)
    {
        HttpContext.Session.SetString("ShipmentDraft", System.Text.Json.JsonSerializer.Serialize(draft));
        return Ok();
    }

    [HttpDelete("draft")]
    public ActionResult ClearDraft()
    {
        HttpContext.Session.Remove("ShipmentDraft");
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] CreateShipmentRequest request)
    {
        var shipment = await _db.Shipments.FindAsync(id);
        if (shipment is null) return NotFound();
        if (!User.IsAdmin() && shipment.UserId != User.GetUserId()) return Forbid();
        if (shipment.ShipmentStatus != "Booked") return BadRequest(new { message = "Only booked shipments can be modified." });

        shipment.ProductDesc = request.ProductDesc.Trim();
        shipment.Quantity = request.Quantity;
        shipment.InvoiceValue = request.InvoiceValue;
        shipment.Currency = request.Currency;
        shipment.Notes = request.Notes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var shipment = await _db.Shipments.FindAsync(id);
        if (shipment is null) return NotFound();
        if (!User.IsAdmin() && shipment.UserId != User.GetUserId()) return Forbid();
        if (shipment.ShipmentStatus == "Cleared") return BadRequest(new { message = "Cannot delete a cleared shipment." });

        shipment.ShipmentStatus = "Cancelled";
        _db.ShipmentStatusHistory.Add(new ShipmentStatusHistory
        {
            ShipmentId = id,
            Status = "Cancelled",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = User.GetUserId(),
            Remarks = "Cancelled by user"
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
