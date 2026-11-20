namespace TradeEase.Models.Entities;

public class Shipment
{
    public int ShipmentId { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? PartyId { get; set; }
    public int? HSCodeId { get; set; }
    public string ShipmentType { get; set; } = string.Empty;
    public string ProductDesc { get; set; } = string.Empty;
    public decimal InvoiceValue { get; set; }
    public string Currency { get; set; } = "USD";
    public string? OriginPort { get; set; }
    public string? OriginCountry { get; set; }
    public string? DestinationPort { get; set; }
    public string? DestinationCountry { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? TotalValuePKR { get; set; }
    public string ShipmentStatus { get; set; } = "Booked";
    public DateOnly? ShipmentDate { get; set; }
    public DateOnly? ClearanceDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public TradeParty? Party { get; set; }
    public HSCode? HSCode { get; set; }
    public ICollection<ShipmentStatusHistory> StatusHistory { get; set; } = new List<ShipmentStatusHistory>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<DutyCalculation> DutyCalculations { get; set; } = new List<DutyCalculation>();
    public ICollection<SchemeEligibilityResult> SchemeResults { get; set; } = new List<SchemeEligibilityResult>();
}
