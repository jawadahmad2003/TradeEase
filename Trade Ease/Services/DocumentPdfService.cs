using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeEase.Data;
using TradeDocument = TradeEase.Models.Entities.Document;

namespace TradeEase.Services;

public interface IDocumentPdfService
{
    Task<(string RelativePath, int SizeKb)> GenerateAndSaveAsync(int documentId, CancellationToken ct = default);
    Task<byte[]> GetOrCreatePdfBytesAsync(TradeDocument document, CancellationToken ct = default);
}

public class DocumentPdfService : IDocumentPdfService
{
    private readonly TradeEaseDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentPdfService> _logger;

    public DocumentPdfService(TradeEaseDbContext db, IWebHostEnvironment env, ILogger<DocumentPdfService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(string RelativePath, int SizeKb)> GenerateAndSaveAsync(int documentId, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .Include(d => d.Shipment).ThenInclude(s => s!.Party)
            .Include(d => d.Shipment).ThenInclude(s => s!.HSCode)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct)
            ?? throw new InvalidOperationException("Document not found.");

        var bytes = await BuildPdfAsync(doc, ct);
        var relative = SavePdf(doc.UserId, doc.DocumentId, bytes);
        doc.FilePath = relative;
        doc.FileSizeKb = Math.Max(1, bytes.Length / 1024);
        await _db.SaveChangesAsync(ct);
        return (relative, doc.FileSizeKb.Value);
    }

    public async Task<byte[]> GetOrCreatePdfBytesAsync(TradeDocument document, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(document.FilePath))
        {
            var full = GetFullPath(document.FilePath);
            if (File.Exists(full))
                return await File.ReadAllBytesAsync(full, ct);
        }

        var doc = await _db.Documents
            .Include(d => d.Shipment).ThenInclude(s => s!.Party)
            .Include(d => d.Shipment).ThenInclude(s => s!.HSCode)
            .Include(d => d.User)
            .FirstAsync(d => d.DocumentId == document.DocumentId, ct);

        return await BuildPdfAsync(doc, ct);
    }

    private async Task<byte[]> BuildPdfAsync(TradeDocument doc, CancellationToken ct)
    {
        await Task.CompletedTask;
        var shipment = doc.Shipment;
        var user = doc.User;
        var hash = shipment?.HSCode is not null
            ? TradeProductHash.Compute(shipment.HSCode.Code, shipment.HSCode.Description)
            : TradeProductHash.Compute(doc.DocumentType, doc.DocumentName);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("TradeEase").Bold().FontColor(Colors.Green.Darken2).FontSize(22);
                    col.Item().Text(doc.DocumentType).SemiBold().FontSize(16);
                    col.Item().Text($"Generated {doc.UploadedAt:dd-MMM-yyyy HH:mm} UTC").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Text($"Document: {doc.DocumentName}").Bold();
                    col.Item().Text($"Document ID: DOC-{doc.DocumentId}");
                    col.Item().Text($"Exporter: {user.BusinessName ?? user.FullName}");
                    col.Item().Text($"NTN: {user.NTN ?? "—"}  |  CNIC: {user.CNIC ?? "—"}");
                    col.Item().PaddingTop(10).LineHorizontal(1);

                    if (shipment is not null)
                    {
                        col.Item().PaddingTop(10).Text("Shipment Details").Bold().FontSize(13);
                        col.Item().Text($"Reference: {shipment.ShipmentReference}");
                        col.Item().Text($"Type: {shipment.ShipmentType}");
                        col.Item().Text($"Product: {shipment.ProductDesc}");
                        col.Item().Text($"HS Code: {shipment.HSCode?.Code ?? "—"}");
                        col.Item().Text($"Route: {shipment.OriginPort} → {shipment.DestinationPort}");
                        col.Item().Text($"Invoice: {shipment.Currency} {shipment.InvoiceValue:N2}");
                        col.Item().Text($"PKR Value: {(shipment.TotalValuePKR.HasValue ? $"PKR {shipment.TotalValuePKR:N2}" : "—")}");
                        col.Item().Text($"Party: {shipment.Party?.Name ?? "—"}");
                    }

                    if (!string.IsNullOrWhiteSpace(doc.Notes))
                        col.Item().PaddingTop(8).Text($"Notes: {doc.Notes}");

                    col.Item().PaddingTop(12).Text($"Product Hash: {hash}").FontFamily("Courier New").FontSize(9);

                    if (doc.DocumentType == "Packing List" && shipment is not null)
                    {
                        col.Item().PaddingTop(12).Text("Packing Summary").Bold();
                        col.Item().Text($"Quantity: {shipment.Quantity} {shipment.UnitOfMeasure}");
                        col.Item().Text("Packages: As per commercial invoice");
                    }

                    if (doc.DocumentType == "Commercial Invoice" && shipment is not null)
                    {
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Description").Bold();
                                h.Cell().Background(Colors.Green.Lighten4).Padding(4).Text("Amount").Bold();
                            });
                            table.Cell().Padding(4).Text(shipment.ProductDesc);
                            table.Cell().Padding(4).Text($"{shipment.Currency} {shipment.InvoiceValue:N2}");
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("TradeEase — Pakistan Import/Export Platform · ");
                    text.Span("Estimate only — official filing via WeBOC/FBR").FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private string SavePdf(int userId, int documentId, byte[] bytes)
    {
        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "documents", userId.ToString());
        Directory.CreateDirectory(dir);
        var fileName = $"{documentId}.pdf";
        var full = Path.Combine(dir, fileName);
        File.WriteAllBytes(full, bytes);
        return Path.Combine("App_Data", "documents", userId.ToString(), fileName).Replace('\\', '/');
    }

    private string GetFullPath(string relativePath) =>
        Path.Combine(_env.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
