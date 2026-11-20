namespace TradeEase.Models.Entities;

public class EligibilityAnswer
{
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public int? ShipmentId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public string? AnswerValue { get; set; }
    public bool? IsTrue { get; set; }
    public decimal? ScoreContribution { get; set; }
    public DateTime AnsweredAt { get; set; }

    public User User { get; set; } = null!;
    public Shipment? Shipment { get; set; }
    public EligibilityQuestion Question { get; set; } = null!;
}
