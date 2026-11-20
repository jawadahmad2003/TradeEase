using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly TradeEaseDbContext _db;

    public UsersController(TradeEaseDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToListAsync();
        return View(users);
    }
}
