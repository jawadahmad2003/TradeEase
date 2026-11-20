namespace TradeEase.Models.Entities;

public class ForexRate
{
    public int RateId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateOnly RateDate { get; set; }
    public string RateSource { get; set; } = "frankfurter.app";
    public DateTime CreatedAt { get; set; }

    public ICollection<DutyCalculation> DutyCalculations { get; set; } = new List<DutyCalculation>();
}
