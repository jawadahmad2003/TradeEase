namespace TradeEase.Models.Entities;

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string? NTN { get; set; }
    public string? CNIC { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }
    public bool NotifyShipments { get; set; } = true;
    public bool NotifyForex { get; set; } = true;
    public bool NotifyCompliance { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<TradeParty> TradeParties { get; set; } = new List<TradeParty>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<UserComplianceProgress> ComplianceProgress { get; set; } = new List<UserComplianceProgress>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
