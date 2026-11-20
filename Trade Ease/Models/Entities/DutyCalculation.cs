namespace TradeEase.Models.Entities;

public class DutyCalculation
{
    public int CalculationId { get; set; }
    public int ShipmentId { get; set; }
    public int? HSCodeId { get; set; }
    public int? RateId { get; set; }
    public string DutyType { get; set; } = string.Empty;
    public decimal? DutyRate { get; set; }
    public decimal CalculatedAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public decimal? ExchangeRate { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalDuty { get; set; }
    public decimal? NetPayable { get; set; }
    public DateTime CalculatedAt { get; set; }

    public Shipment Shipment { get; set; } = null!;
    public HSCode? HSCode { get; set; }
    public ForexRate? ForexRate { get; set; }
}
