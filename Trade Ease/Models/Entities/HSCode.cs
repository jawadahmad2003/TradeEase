namespace TradeEase.Models.Entities;

public class HSCode
{
    public int HSCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Chapter { get; set; }
    public string? Heading { get; set; }
    public string? Subheading { get; set; }
    public string? Section { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal CustomsDutyPct { get; set; }
    public decimal SalesTaxPct { get; set; }
    public decimal AITPct { get; set; }
    public string TariffYear { get; set; } = "FY 2025-26";
    public bool IsActive { get; set; } = true;

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<DutyCalculation> DutyCalculations { get; set; } = new List<DutyCalculation>();
}
