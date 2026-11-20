namespace TradeEase.Models.Entities;

public class ExportScheme
{
    public int SchemeId { get; set; }
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string? Agency { get; set; }
    public string? BenefitDetails { get; set; }
    public string? ApplicationDeadline { get; set; }
    public decimal MinExportPct { get; set; }
    public decimal MinTurnoverUsd { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SchemeEligibilityResult> Results { get; set; } = new List<SchemeEligibilityResult>();
}
