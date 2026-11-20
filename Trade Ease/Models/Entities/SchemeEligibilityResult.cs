namespace TradeEase.Models.Entities;

public class SchemeEligibilityResult
{
    public int ResultId { get; set; }
    public int? ShipmentId { get; set; }
    public int UserId { get; set; }
    public int SchemeId { get; set; }
    public bool IsEligible { get; set; }
    public decimal? EligibilityScore { get; set; }
    public DateTime CheckedAt { get; set; }

    public Shipment? Shipment { get; set; }
    public User User { get; set; } = null!;
    public ExportScheme Scheme { get; set; } = null!;
}
