using Microsoft.AspNetCore.Mvc;
using TradeEase.ReportingApi.Services;

namespace TradeEase.ReportingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reporting;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportingService reporting, ILogger<ReportsController> logger)
    {
        _reporting = reporting;
        _logger = logger;
    }

    /// <summary>GET /api/reports/shipments — Dynamic shipment report via stored procedure</summary>
    [HttpGet("shipments")]
    public async Task<IActionResult> GetShipmentReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? shipmentType, 
        [FromQuery] string? status,
        CancellationToken ct)
    {
        try
        {
            var data = await _reporting.GetShipmentReportAsync(startDate, endDate, shipmentType, status, ct);
            return Ok(new { success = true, count = data.Count(), data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating shipment report");
            return StatusCode(500, new { success = false, message = "Failed to generate report." });
        }
    }

    /// <summary>GET /api/reports/top-countries — Top import/export countries</summary>
    [HttpGet("top-countries")]
    public async Task<IActionResult> GetTopCountries([FromQuery] int topN = 5, CancellationToken ct = default)
    {
        try
        {
            var data = await _reporting.GetTopCountriesAsync(topN, ct);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top countries");
            return StatusCode(500, new { success = false, message = "Failed to fetch country data." });
        }
    }

    /// <summary>GET /api/reports/shipment/{id} — Single shipment detail from view</summary>
    [HttpGet("shipment/{id:int}")]
    public async Task<IActionResult> GetShipmentById(int id, CancellationToken ct)
    {
        try
        {
            var data = await _reporting.GetShipmentByIdAsync(id, ct);
            if (data is null)
                return NotFound(new { success = false, message = $"Shipment {id} not found." });
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shipment {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to fetch shipment." });
        }
    }

    /// <summary>GET /api/reports/health — Liveness check</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "TradeEase.ReportingApi", utc = DateTime.UtcNow });
}
