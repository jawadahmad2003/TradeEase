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
public class DocumentsController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly IDocumentPdfService _pdf;

    public DocumentsController(TradeEaseDbContext db, IDocumentPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> List()
    {
        var query = _db.Documents.Include(d => d.Shipment).AsQueryable();
        if (!User.IsAdmin())
            query = query.Where(d => d.UserId == User.GetUserId());

        var docs = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
        return Ok(docs.Select(MapDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Trader")]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentRequest request)
    {
        if (request.ShipmentId.HasValue)
        {
            var shipment = await _db.Shipments.FindAsync(request.ShipmentId.Value);
            if (shipment is null || (!User.IsAdmin() && shipment.UserId != User.GetUserId()))
                return BadRequest(new { message = "Invalid shipment." });
        }

        var doc = new Document
        {
            UserId = User.GetUserId(),
            ShipmentId = request.ShipmentId,
            DocumentType = request.DocumentType.Trim(),
            DocumentName = request.DocumentName.Trim(),
            Status = request.DocumentType == "Uploaded" ? "Uploaded" : "Generated",
            Notes = request.Notes,
            UploadedAt = DateTime.UtcNow,
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        try
        {
            await _pdf.GenerateAndSaveAsync(doc.DocumentId);
            doc = await _db.Documents.Include(d => d.Shipment).FirstAsync(d => d.DocumentId == doc.DocumentId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"PDF generation failed: {ex.Message}" });
        }

        return Ok(MapDto(doc));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var doc = await _db.Documents
            .Include(d => d.Shipment).ThenInclude(s => s!.Party)
            .Include(d => d.Shipment).ThenInclude(s => s!.HSCode)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DocumentId == id);

        if (doc is null) return NotFound(new { message = "Document not found." });
        if (!User.IsAdmin() && doc.UserId != User.GetUserId())
            return Forbid();

        try
        {
            var bytes = await _pdf.GetOrCreatePdfBytesAsync(doc);
            var safeName = string.Join("_", doc.DocumentName.Split(Path.GetInvalidFileNameChars())) + ".pdf";
            return File(bytes, "application/pdf", safeName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"PDF download failed: {ex.Message}" });
        }
    }

    private static DocumentDto MapDto(Document d) => new(
        d.DocumentId,
        d.DocumentType,
        d.DocumentName,
        d.Status,
        d.UploadedAt,
        d.ShipmentId,
        d.Shipment?.ShipmentReference,
        d.FileSizeKb);
}
