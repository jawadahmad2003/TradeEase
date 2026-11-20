namespace TradeEase.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalShipments { get; set; }
    public int PendingShipments { get; set; }
    public int ClearedShipments { get; set; }
    public decimal TotalTradeValue { get; set; }
    public List<ShipmentListItem> RecentShipments { get; set; } = new();
    public List<ForexRateItem> ForexRates { get; set; } = new();
}

public class ShipmentListItem
{
    public string Reference { get; set; } = string.Empty;
    public string ProductDesc { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? TotalValuePKR { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ForexRateItem
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
}
