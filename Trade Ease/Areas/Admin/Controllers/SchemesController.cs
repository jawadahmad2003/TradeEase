using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SchemesController : Controller
{
    private readonly TradeEaseDbContext _db;

    public SchemesController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var schemes = await _db.ExportSchemes.OrderBy(s => s.SchemeCode).ToListAsync();
        return View(schemes);
    }
}
