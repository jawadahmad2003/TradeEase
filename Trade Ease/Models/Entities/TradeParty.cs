namespace TradeEase.Models.Entities;

public class TradeParty
{
    public int PartyId { get; set; }
    public int UserId { get; set; }
    public string PartyType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public string? RegistrationNo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
