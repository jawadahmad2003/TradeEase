namespace TradeEase.Models.Entities;

public class ShipmentStatusHistory
{
    public int HistoryId { get; set; }
    public int ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }

    public Shipment Shipment { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
