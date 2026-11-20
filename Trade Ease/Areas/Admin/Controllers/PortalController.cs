using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class PortalController : Controller
{
    private readonly TradeEaseDbContext _db;

    public PortalController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.ShipmentCount = await _db.Shipments.CountAsync();
        ViewBag.HSCodeCount = await _db.HSCodes.CountAsync(u => u.IsActive);
        ViewBag.PendingUsers = await _db.Users.CountAsync(u => !u.IsVerified);
        return View();
    }
}
