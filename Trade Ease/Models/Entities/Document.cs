namespace TradeEase.Models.Entities;

public class Document
{
    public int DocumentId { get; set; }
    public int? ShipmentId { get; set; }
    public int UserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? FileSizeKb { get; set; }
    public string Status { get; set; } = "Generated";
    public DateTime UploadedAt { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }

    public Shipment? Shipment { get; set; }
    public User User { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}
