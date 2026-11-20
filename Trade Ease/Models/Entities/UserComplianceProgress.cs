namespace TradeEase.Models.Entities;

public class UserComplianceProgress
{
    public int ProgressId { get; set; }
    public int UserId { get; set; }
    public int StepId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public ComplianceStep Step { get; set; } = null!;
}
