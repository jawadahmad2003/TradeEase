using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SystemController : Controller
{
    private readonly TradeEaseDbContext _db;

    public SystemController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var settings = await _db.SystemSettings.ToListAsync();
        var lastForex = await _db.ForexRates.OrderByDescending(f => f.CreatedAt).FirstOrDefaultAsync();
        ViewBag.LastForex = lastForex;
        return View(settings);
    }
}
