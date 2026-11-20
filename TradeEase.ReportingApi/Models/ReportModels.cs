namespace TradeEase.ReportingApi.Models;

public class ShipmentReportModel
{
    public int ShipmentId { get; set; }
    public string ShipmentReference { get; set; } = string.Empty;
    public string ShipmentType { get; set; } = string.Empty;
    public string ShipmentStatus { get; set; } = string.Empty;
    public decimal InvoiceValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? OriginCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? OriginPort { get; set; }
    public string? DestinationPort { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public DateTime? ClearanceDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserBusinessName { get; set; }
    
    public string? PartyName { get; set; }
    public string? PartyType { get; set; }
    public string? PartyCountry { get; set; }
    
    public string? HSCode { get; set; }
    public string? HSDescription { get; set; }
    public string? HSChapter { get; set; }
    public decimal? CustomsDutyPct { get; set; }
    public decimal? SalesTaxPct { get; set; }
}

public class CountryAggregationModel
{
    public string ShipmentType { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int TotalShipments { get; set; }
    public decimal TotalValue { get; set; }
}
