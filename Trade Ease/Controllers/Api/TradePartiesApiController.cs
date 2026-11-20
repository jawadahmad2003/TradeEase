using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class TradePartiesController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;

    public TradePartiesController(TradeEaseDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TradePartyDto>>> List()
    {
        var query = _db.TradeParties.AsQueryable();
        if (!User.IsAdmin())
            query = query.Where(p => p.UserId == User.GetUserId());

        var parties = await query.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(parties.Select(p => new TradePartyDto(
            p.PartyId, p.PartyType, p.Name, p.City, p.Country, p.Email, p.Phone, p.TaxId, p.IsActive)));
    }

    [HttpPost]
    [Authorize(Roles = "Trader")]
    public async Task<ActionResult<TradePartyDto>> Create([FromBody] CreateTradePartyRequest request)
    {
        var party = new TradeParty
        {
            UserId = User.GetUserId(),
            PartyType = request.PartyType,
            Name = request.Name.Trim(),
            City = request.City,
            Country = request.Country,
            Address = request.Address,
            Email = request.Email,
            Phone = request.Phone,
            TaxId = request.TaxId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _db.TradeParties.Add(party);
        await _db.SaveChangesAsync();

        return Ok(new TradePartyDto(
            party.PartyId, party.PartyType, party.Name, party.City, party.Country,
            party.Email, party.Phone, party.TaxId, party.IsActive));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Trader")]
    public async Task<ActionResult> Update(int id, [FromBody] CreateTradePartyRequest request)
    {
        var party = await _db.TradeParties.FindAsync(id);
        if (party is null) return NotFound();
        if (!User.IsAdmin() && party.UserId != User.GetUserId()) return Forbid();

        party.PartyType = request.PartyType;
        party.Name = request.Name.Trim();
        party.City = request.City;
        party.Country = request.Country;
        party.Address = request.Address;
        party.Email = request.Email;
        party.Phone = request.Phone;
        party.TaxId = request.TaxId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Trader")]
    public async Task<ActionResult> Delete(int id)
    {
        var party = await _db.TradeParties.FindAsync(id);
        if (party is null) return NotFound();
        if (!User.IsAdmin() && party.UserId != User.GetUserId()) return Forbid();

        // Soft delete
        party.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
