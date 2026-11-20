using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ShipmentsController : Controller
{
    private readonly TradeEaseDbContext _db;

    public ShipmentsController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var shipments = await _db.Shipments
            .Include(s => s.User)
            .Include(s => s.Party)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return View(shipments);
    }
}
