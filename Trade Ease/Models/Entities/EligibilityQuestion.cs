namespace TradeEase.Models.Entities;

public class EligibilityQuestion
{
    public int QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionCategory { get; set; }
    public string QuestionType { get; set; } = string.Empty;

    public ICollection<EligibilityAnswer> Answers { get; set; } = new List<EligibilityAnswer>();
}
