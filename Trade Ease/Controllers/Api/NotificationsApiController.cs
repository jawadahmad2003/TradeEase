using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly ILiveForexService _forex;

    public NotificationsController(TradeEaseDbContext db, ILiveForexService forex)
    {
        _db = db;
        _forex = forex;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> List()
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var items = new List<NotificationDto>();

        if (isAdmin)
        {
            var pendingCount = await _db.Shipments.AsNoTracking()
                .CountAsync(s => s.ShipmentStatus == "Under Customs" || s.ShipmentStatus == "At Port");

            if (pendingCount > 0)
            {
                items.Add(new NotificationDto(
                    "admin-customs-summary",
                    $"{pendingCount} shipments are awaiting customs review",
                    DateTime.UtcNow,
                    "shipment",
                    null));
            }
        }
        else
        {
            var pendingCustoms = await _db.Shipments.AsNoTracking()
                .Where(s => s.UserId == userId)
                .Where(s => s.ShipmentStatus == "Under Customs" || s.ShipmentStatus == "At Port")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.ShipmentId, s.ShipmentReference, s.CreatedAt })
                .Take(5)
                .ToListAsync();

            foreach (var s in pendingCustoms)
            {
                items.Add(new NotificationDto(
                    $"customs-{s.ShipmentId}",
                    $"Customs action needed for {s.ShipmentReference}",
                    s.CreatedAt,
                    "shipment",
                    s.ShipmentId));
            }

            var steps = await _db.ComplianceSteps.AsNoTracking().OrderBy(s => s.StepOrder).ToListAsync();
            var progress = await _db.UserComplianceProgress.AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.StepId);

            foreach (var step in steps.Where(s => !(progress.GetValueOrDefault(s.StepId)?.IsCompleted ?? false)).Take(3))
            {
                items.Add(new NotificationDto(
                    $"compliance-{step.StepId}",
                    $"Compliance step pending: {step.StepName}",
                    DateTime.UtcNow,
                    "compliance",
                    step.StepId));
            }

            var recentDocs = await _db.Documents.AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new
                {
                    d.DocumentId,
                    d.DocumentType,
                    d.DocumentName,
                    d.UploadedAt,
                    d.ShipmentId,
                    ShipmentReference = d.Shipment != null ? d.Shipment.ShipmentReference : null,
                })
                .Take(3)
                .ToListAsync();

            foreach (var doc in recentDocs)
            {
                items.Add(new NotificationDto(
                    $"doc-{doc.DocumentId}",
                    $"{doc.DocumentType}: {doc.DocumentName}" +
                    (doc.ShipmentReference is not null ? $" ({doc.ShipmentReference})" : ""),
                    doc.UploadedAt,
                    "document",
                    doc.DocumentId));
            }
        }

        var snapshot = _forex.GetSnapshot();
        if (snapshot.Rates.Count > 0)
        {
            items.Add(new NotificationDto(
                "forex-live",
                $"Live forex updated ({snapshot.Rates.Count} currencies) at {snapshot.UpdatedUtc:HH:mm} UTC",
                snapshot.UpdatedUtc,
                "forex",
                null));
        }

        return Ok(items.OrderByDescending(n => n.OccurredAt).Take(15));
    }
}
