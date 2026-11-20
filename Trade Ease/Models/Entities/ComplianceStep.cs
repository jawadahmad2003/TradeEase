using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeEase.Models.Entities;

[Table("ComplianceSteps")]
public class ComplianceStep
{
    [Key]
    public int StepId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EstimatedTime { get; set; }
    public string? PortalUrl { get; set; }

    public ICollection<UserComplianceProgress> UserProgress { get; set; } = new List<UserComplianceProgress>();
}
