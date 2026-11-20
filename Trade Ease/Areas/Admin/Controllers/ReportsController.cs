using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController : Controller
{
    private readonly TradeEaseDbContext _db;

    public ReportsController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.ShipmentCount = await _db.Shipments.CountAsync();
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.DocumentCount = await _db.Documents.CountAsync();
        ViewBag.DutyCount = await _db.DutyCalculations.CountAsync();
        return View();
    }
}
