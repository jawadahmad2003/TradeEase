using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly IConfiguration _config;

    public HealthController(TradeEaseDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var dbOk = false;
        string? dbError = null;
        int tableCount = 0;

        try
        {
            dbOk = await _db.Database.CanConnectAsync();
            if (dbOk)
                tableCount = await _db.Roles.CountAsync();
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        return Ok(new
        {
            status = dbOk ? "healthy" : "degraded",
            database = new
            {
                connected = dbOk,
                name = "TradeEaseDB",
                server = _config.GetConnectionString("DefaultConnection")?.Split(';').FirstOrDefault(s => s.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))?.Replace("Server=", ""),
                error = dbError,
                seedOk = tableCount >= 2,
            },
            forexApi = new
            {
                provider = "frankfurter.app",
                apiKeyRequired = false,
                url = _config["ForexApi:BaseUrl"] ?? "https://api.frankfurter.app",
            },
            timestamp = DateTime.UtcNow,
        });
    }
}
