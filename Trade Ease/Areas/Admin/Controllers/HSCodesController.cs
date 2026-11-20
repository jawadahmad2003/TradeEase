using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class HSCodesController : Controller
{
    private readonly TradeEaseDbContext _db;

    public HSCodesController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var codes = await _db.HSCodes.OrderBy(h => h.Code).ToListAsync();
        return View(codes);
    }
}
