using Microsoft.AspNetCore.Mvc;
using TradeEase.ReportingApi.Services;
using System.Text;

namespace TradeEase.ReportingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IReportingService _reporting;

    public ExportController(IReportingService reporting)
    {
        _reporting = reporting;
    }

    [HttpGet("{type}")]
    public async Task<IActionResult> Export(string type)
    {
        if (type.ToLowerInvariant() == "shipments")
        {
            var data = await _reporting.GetShipmentReportAsync(null, null, null, null);
            var sb = new StringBuilder();
            sb.AppendLine("Reference,Trader,Type,Status,Currency,InvoiceValue,CreatedAt");
            foreach (var s in data)
            {
                sb.AppendLine($"{s.ShipmentReference},{CsvEscape(s.UserFullName)},{s.ShipmentType},{s.ShipmentStatus},{s.Currency},{s.InvoiceValue:F2},{s.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"tradeease-shipments-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        
        return BadRequest("Export type not supported in this microservice yet.");
    }

    private static string CsvEscape(object? value)
    {
        var s = value?.ToString() ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
