namespace TradeEase.Models.Entities;

public class SystemSetting
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public User? UpdatedByUser { get; set; }
}
