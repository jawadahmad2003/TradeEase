using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;
using TradeEase.Services;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;
    private readonly ILiveForexService _liveForex;
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminController(TradeEaseDbContext db, ILiveForexService liveForex, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _liveForex = liveForex;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats()
    {
        var totalUsers = await _db.Users.AsNoTracking().CountAsync();
        var activeTraders = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive && u.Role.RoleName == "Trader");
        var totalShipments = await _db.Shipments.AsNoTracking().CountAsync();
        var pendingShipments = await _db.Shipments.AsNoTracking().CountAsync(s => s.ShipmentStatus != "Cleared");
        var totalHsCodes = await _db.HSCodes.AsNoTracking().CountAsync(h => h.IsActive);
        var activeSchemes = await _db.ExportSchemes.AsNoTracking().CountAsync(s => s.IsActive);

        return Ok(new AdminStatsDto(
            totalUsers,
            activeTraders,
            totalShipments,
            pendingShipments,
            totalHsCodes,
            activeSchemes));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserAdminDto>>> Users()
    {
        var users = await _db.Users.AsNoTracking().Include(u => u.Role).OrderByDescending(u => u.CreatedAt).ToListAsync();
        return Ok(users.Select(u => new UserAdminDto(
            u.UserId, u.FullName, u.Email, u.Role.RoleName,
            u.BusinessName, u.IsActive, u.IsVerified, u.CreatedAt, u.LastLogin)));
    }

    [HttpPatch("users/{id:int}")]
    public async Task<ActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("shipments")]
    public async Task<ActionResult<IEnumerable<AdminShipmentDto>>> Shipments()
    {
        var list = await _db.Shipments
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(s => new AdminShipmentDto(
            s.ShipmentId,
            s.ShipmentReference,
            s.User.FullName,
            s.User.BusinessName,
            s.ShipmentType,
            s.ProductDesc,
            s.TotalValuePKR.HasValue ? $"PKR {(s.TotalValuePKR.Value / 1_000_000m):0.#}M" : $"{s.Currency} {s.InvoiceValue:N0}",
            s.ShipmentStatus,
            s.CreatedAt)));
    }

    [HttpGet("hscodes")]
    public async Task<ActionResult<IEnumerable<HSCodeDto>>> HSCodes()
    {
        var codes = await _db.HSCodes.AsNoTracking().OrderBy(h => h.Code).ToListAsync();
        return Ok(codes.Select(MapHsCode));
    }

    [HttpPost("hscodes")]
    public async Task<ActionResult<HSCodeDto>> CreateHSCode([FromBody] CreateHSCodeRequest request)
    {
        if (await _db.HSCodes.AnyAsync(h => h.Code == request.Code.Trim()))
            return BadRequest(new { message = "HS code already exists." });

        var hs = new HSCode
        {
            Code = request.Code.Trim(),
            Description = request.Description.Trim(),
            CustomsDutyPct = request.CustomsDutyPct,
            SalesTaxPct = request.SalesTaxPct,
            AITPct = request.AitPct,
            UnitOfMeasure = request.UnitOfMeasure,
            TariffYear = "FY 2025-26",
            IsActive = true,
        };
        _db.HSCodes.Add(hs);
        await _db.SaveChangesAsync();

        return Ok(MapHsCode(hs));
    }

    [HttpPut("hscodes/{id:int}")]
    public async Task<ActionResult<HSCodeDto>> UpdateHSCode(int id, [FromBody] CreateHSCodeRequest request)
    {
        var hs = await _db.HSCodes.FindAsync(id);
        if (hs is null) return NotFound();

        // Check if code is changed and already exists
        if (hs.Code != request.Code.Trim() && await _db.HSCodes.AnyAsync(h => h.Code == request.Code.Trim() && h.HSCodeId != id))
            return BadRequest(new { message = "Another HS code with this code already exists." });

        hs.Code = request.Code.Trim();
        hs.Description = request.Description.Trim();
        hs.CustomsDutyPct = request.CustomsDutyPct;
        hs.SalesTaxPct = request.SalesTaxPct;
        hs.AITPct = request.AitPct;
        hs.UnitOfMeasure = request.UnitOfMeasure;

        await _db.SaveChangesAsync();
        return Ok(MapHsCode(hs));
    }

    [HttpDelete("hscodes/{id:int}")]
    public async Task<ActionResult> DeleteHSCode(int id)
    {
        var hs = await _db.HSCodes.FindAsync(id);
        if (hs is null) return NotFound();
        hs.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("schemes")]
    public async Task<ActionResult<IEnumerable<SchemeDto>>> Schemes()
    {
        var schemes = await _db.ExportSchemes.AsNoTracking().OrderBy(s => s.SchemeCode).ToListAsync();
        return Ok(schemes.Select(s => new SchemeDto(
            s.SchemeId, s.SchemeCode, s.SchemeName, s.Agency, s.BenefitDetails,
            s.ApplicationDeadline, s.MinExportPct, s.MinTurnoverUsd, s.IsActive)));
    }

    [HttpPost("schemes")]
    public async Task<ActionResult<SchemeDto>> CreateScheme([FromBody] CreateSchemeRequest request)
    {
        if (await _db.ExportSchemes.AnyAsync(s => s.SchemeCode == request.SchemeCode.Trim()))
            return BadRequest(new { message = "Scheme code already exists." });

        var scheme = new ExportScheme
        {
            SchemeCode = request.SchemeCode.Trim(),
            SchemeName = request.SchemeName.Trim(),
            Agency = request.Agency?.Trim(),
            BenefitDetails = request.BenefitDetails?.Trim(),
            ApplicationDeadline = request.ApplicationDeadline?.Trim(),
            MinExportPct = request.MinExportPct,
            MinTurnoverUsd = request.MinTurnoverUsd,
            IsActive = request.IsActive
        };
        _db.ExportSchemes.Add(scheme);
        await _db.SaveChangesAsync();

        return Ok(new SchemeDto(
            scheme.SchemeId, scheme.SchemeCode, scheme.SchemeName, scheme.Agency, scheme.BenefitDetails,
            scheme.ApplicationDeadline, scheme.MinExportPct, scheme.MinTurnoverUsd, scheme.IsActive));
    }

    [HttpPut("schemes")]
    public async Task<ActionResult> UpdateSchemes([FromBody] IEnumerable<UpdateSchemeRequest> updates)
    {
        foreach (var u in updates)
        {
            var scheme = await _db.ExportSchemes.FirstOrDefaultAsync(s => s.SchemeCode == u.SchemeCode);
            if (scheme is null) continue;
            scheme.MinExportPct = u.MinExportPct;
            scheme.MinTurnoverUsd = u.MinTurnoverUsd;
            scheme.IsActive = u.IsActive;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("schemes/{id:int}")]
    public async Task<ActionResult> DeleteScheme(int id)
    {
        var scheme = await _db.ExportSchemes.FindAsync(id);
        if (scheme is null) return NotFound();
        scheme.IsActive = false; // Soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SystemSettingsDto>> Settings()
    {
        var settings = await _db.SystemSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);
        var snapshot = _liveForex.GetSnapshot();

        return Ok(new SystemSettingsDto(
            settings.GetValueOrDefault("TariffYear", "FY 2025-26"),
            settings.GetValueOrDefault("MaintenanceMode", "false") == "true",
            snapshot.Rates.Count > 0
                ? snapshot.UpdatedUtc.ToString("dd-MMM-yyyy HH:mm") + " UTC (live)"
                : "Pending first refresh",
            settings.GetValueOrDefault("AppVersion", "1.0.0")));
    }

    [HttpPut("settings")]
    public async Task<ActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        if (request.TariffYear is not null)
            await UpsertSetting("TariffYear", request.TariffYear);
        if (request.MaintenanceMode.HasValue)
            await UpsertSetting("MaintenanceMode", request.MaintenanceMode.Value ? "true" : "false");
        return NoContent();
    }

    [HttpPost("forex/refresh")]
    public async Task<ActionResult<ForexLiveResponseDto>> RefreshForex(CancellationToken ct)
    {
        await _liveForex.RefreshAsync(ct);
        var snapshot = _liveForex.GetSnapshot();
        return Ok(new ForexLiveResponseDto(
            snapshot.Rates.Select(r => new ForexRateDto(
                r.CurrencyCode, r.ExchangeRate,
                snapshot.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                r.ChangePct, r.IsUp)).ToList(),
            snapshot.UpdatedUtc,
            snapshot.Source,
            snapshot.RefreshIntervalSeconds));
    }

    [HttpGet("reports")]
    public async Task<ActionResult<AdminReportsDto>> Reports()
    {
        var shipmentCount = await _db.Shipments.CountAsync();
        var userCount = await _db.Users.CountAsync();
        var dutyCount = await _db.DutyCalculations.CountAsync();
        var schemeChecks = await _db.SchemeEligibilityResults.CountAsync(r => r.IsEligible);
        var docCount = await _db.Documents.CountAsync();

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var monthly = await _db.Shipments
            .AsNoTracking()
            .Where(s => s.CreatedAt >= sixMonthsAgo)
            .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var usage = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var d = DateTime.UtcNow.AddMonths(-5 + i);
                var match = monthly.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                return new MonthlyUsageDto(monthNames[d.Month - 1], match?.Count ?? 0);
            })
            .ToList();

        var reports = new List<AdminReportDto>
        {
            new("Monthly Shipment Summary", "All import/export shipments by month", shipmentCount, "shipments"),
            new("Trader Activity Report", "Registered users and trader accounts", userCount, "users"),
            new("Duty Calculation Log", "HS duty calculations performed", dutyCount, "duty"),
            new("Scheme Eligibility Checks", "EFS/DDT checks run by traders", schemeChecks, "schemes"),
            new("Document Generation Log", "Invoices and packing lists generated", docCount, "documents"),
        };

        return Ok(new AdminReportsDto(reports, usage));
    }

    [HttpGet("reports/export/{type}")]
    public async Task<IActionResult> ExportReport(string type, [FromQuery] string format = "xlsx")
    {
        // Build data rows (header + data)
        var (title, headers, rows) = type.ToLowerInvariant() switch
        {
            "shipments" => await BuildShipmentsData(),
            "users"     => await BuildUsersData(),
            "duty"      => await BuildDutyData(),
            "schemes"   => await BuildSchemesData(),
            "documents" => await BuildDocumentsData(),
            _           => (null, null, null),
        };

        if (title is null || headers is null || rows is null)
            return BadRequest(new { message = "Unknown report type." });

        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            // Use community license for QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TradeEase ERP").FontSize(16).Bold().FontColor("#1a7a5e");
                                c.Item().Text($"{title} — FY 2025-26").FontSize(11).SemiBold();
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                                c.Item().Text("CONFIDENTIAL — READ ONLY").FontSize(7).Bold().FontColor(Colors.Red.Medium);
                            });
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#1a7a5e");
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var _ in headers)
                                cols.RelativeColumn();
                        });

                        // Header row
                        table.Header(header =>
                        {
                            foreach (var h in headers)
                                header.Cell().Background("#1a7a5e").Padding(4)
                                    .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                        });

                        // Data rows
                        bool alt = false;
                        foreach (var rowData in rows)
                        {
                            var bg = alt ? "#f0faf6" : "#ffffff";
                            foreach (var cell in rowData)
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3).Text(cell ?? "").FontSize(8);
                            alt = !alt;
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("TradeEase — Pakistan Import & Export Management · Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(7);
                        t.Span(" of ").FontSize(7);
                        t.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"TradeEase-{title.Replace(" ","-")}-{date}.pdf");
        }
        else
        {
            // Default: locked Excel
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet(title);

            // Title row
            ws.Cell(1, 1).Value = $"TradeEase — {title}";
            var titleRange = ws.Range(1, 1, 1, headers.Length);
            titleRange.Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1a7a5e");

            // Sub-title row
            ws.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC  |  CONFIDENTIAL — READ ONLY";
            ws.Range(2, 1, 2, headers.Length).Merge();
            ws.Cell(2, 1).Style.Font.FontSize = 9;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            // Header row
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(4, i + 1);
                c.Value = headers[i];
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a7a5e");
                c.Style.Font.FontColor = XLColor.White;
                c.Style.Font.Bold = true;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Data rows
            int rowIdx = 5;
            bool altRow = false;
            foreach (var rowData in rows)
            {
                for (int i = 0; i < rowData.Length; i++)
                {
                    var c = ws.Cell(rowIdx, i + 1);
                    c.Value = rowData[i] ?? "";
                    c.Style.Fill.BackgroundColor = altRow ? XLColor.FromHtml("#f0faf6") : XLColor.White;
                    c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    c.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e0e0e0");
                }
                altRow = !altRow;
                rowIdx++;
            }

            ws.Columns().AdjustToContents();

            // Lock the sheet — read-only protection
            ws.Protect("TradeEase2025");

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TradeEase-{title.Replace(" ","-")}-{date}.xlsx");
        }
    }

    // ── Data builders — shared between Excel & PDF ────────────────────────────

    private async Task<(string Title, string[] Headers, List<string[]> Rows)> BuildShipmentsData()
    {
        var list = await _db.Shipments.Include(s => s.User).OrderByDescending(s => s.CreatedAt).ToListAsync();
        var headers = new[] { "Reference", "Trader", "Business", "Type", "Product", "Status", "Currency", "Invoice Value", "Created At" };
        var rows = list.Select(s => new[]
        {
            s.ShipmentReference, s.User.FullName, s.User.BusinessName ?? "",
            s.ShipmentType, s.ProductDesc, s.ShipmentStatus, s.Currency,
            s.InvoiceValue.ToString("F2"), s.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
        return ("Monthly Shipment Summary", headers, rows);
    }

    private async Task<(string Title, string[] Headers, List<string[]> Rows)> BuildUsersData()
    {
        var list = await _db.Users.Include(u => u.Role).OrderByDescending(u => u.CreatedAt).ToListAsync();
        var headers = new[] { "User ID", "Full Name", "Email", "Role", "Business", "Active", "Verified", "Created At", "Last Login" };
        var rows = list.Select(u => new[]
        {
            u.UserId.ToString(), u.FullName, u.Email, u.Role.RoleName,
            u.BusinessName ?? "", u.IsActive ? "Yes" : "No", u.IsVerified ? "Yes" : "No",
            u.CreatedAt.ToString("yyyy-MM-dd HH:mm"), u.LastLogin?.ToString("yyyy-MM-dd HH:mm") ?? "",
        }).ToList();
        return ("Trader Activity Report", headers, rows);
    }

    private async Task<(string Title, string[] Headers, List<string[]> Rows)> BuildDutyData()
    {
        var list = await _db.DutyCalculations.Include(d => d.Shipment).Include(d => d.HSCode)
            .OrderByDescending(d => d.CalculatedAt).ToListAsync();
        var headers = new[] { "ID", "Shipment", "HS Code", "Duty Type", "Duty %", "Total Duty", "Total Tax", "Net Payable", "Currency", "Exchange Rate", "Calculated At" };
        var rows = list.Select(d => new[]
        {
            d.CalculationId.ToString(), d.Shipment.ShipmentReference, d.HSCode?.Code ?? "",
            d.DutyType, d.DutyRate?.ToString("F2") ?? "", d.TotalDuty?.ToString("F2") ?? "",
            d.TotalTax?.ToString("F2") ?? "", d.NetPayable?.ToString("F2") ?? "",
            d.Currency, d.ExchangeRate?.ToString("F4") ?? "", d.CalculatedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
        return ("Duty Calculation Log", headers, rows);
    }

    private async Task<(string Title, string[] Headers, List<string[]> Rows)> BuildSchemesData()
    {
        var list = await _db.SchemeEligibilityResults.Include(r => r.User).Include(r => r.Scheme)
            .Include(r => r.Shipment).OrderByDescending(r => r.CheckedAt).ToListAsync();
        var headers = new[] { "ID", "Trader", "Scheme", "Shipment", "Eligible", "Score", "Checked At" };
        var rows = list.Select(r => new[]
        {
            r.ResultId.ToString(), r.User.FullName, r.Scheme.SchemeCode,
            r.Shipment?.ShipmentReference ?? "", r.IsEligible ? "Yes" : "No",
            r.EligibilityScore?.ToString("F1") ?? "", r.CheckedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
        return ("Scheme Eligibility Checks", headers, rows);
    }

    private async Task<(string Title, string[] Headers, List<string[]> Rows)> BuildDocumentsData()
    {
        var list = await _db.Documents.Include(d => d.User).Include(d => d.Shipment)
            .OrderByDescending(d => d.UploadedAt).ToListAsync();
        var headers = new[] { "ID", "Type", "Name", "Status", "Trader", "Shipment", "Size KB", "Uploaded At" };
        var rows = list.Select(d => new[]
        {
            d.DocumentId.ToString(), d.DocumentType, d.DocumentName, d.Status, d.User.FullName,
            d.Shipment?.ShipmentReference ?? "", d.FileSizeKb?.ToString() ?? "", d.UploadedAt.ToString("yyyy-MM-dd HH:mm"),
        }).ToList();
        return ("Document Generation Log", headers, rows);
    }

    private async Task UpsertSetting(string key, string value)
    {
        var setting = await _db.SystemSettings.FindAsync(key);
        if (setting is null)
        {
            _db.SystemSettings.Add(new SystemSetting
            {
                SettingKey = key,
                SettingValue = value,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = User.GetUserId(),
            });
        }
        else
        {
            setting.SettingValue = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = User.GetUserId();
        }
        await _db.SaveChangesAsync();
    }

    private static HSCodeDto MapHsCode(HSCode h) => new(
        h.HSCodeId, h.Code, h.Description, h.UnitOfMeasure,
        h.CustomsDutyPct, h.SalesTaxPct, h.AITPct, h.TariffYear,
        TradeProductHash.Compute(h.Code, h.Description));

    // ─── Reporting API Proxy Endpoints ──────────────────────────────────────

    /// <summary>Proxy → GET TradeEase.ReportingApi /api/reports/shipments</summary>
    [HttpGet("reports/shipments")]
    public async Task<IActionResult> ReportShipments(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? shipmentType,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ReportingApi");
        var qs = $"?startDate={startDate:s}&endDate={endDate:s}&shipmentType={shipmentType}&status={status}";
        try
        {
            var resp = await client.GetAsync($"api/reports/shipments{qs}", ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Reporting API unavailable.", detail = ex.Message });
        }
    }

    /// <summary>Proxy → GET TradeEase.ReportingApi /api/reports/top-countries</summary>
    [HttpGet("reports/top-countries")]
    public async Task<IActionResult> ReportTopCountries([FromQuery] int topN = 5, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("ReportingApi");
        try
        {
            var resp = await client.GetAsync($"api/reports/top-countries?topN={topN}", ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Reporting API unavailable.", detail = ex.Message });
        }
    }

    /// <summary>Proxy → GET TradeEase.ReportingApi /api/reports/shipment/{id}</summary>
    [HttpGet("reports/shipment/{id:int}")]
    public async Task<IActionResult> ReportShipmentById(int id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("ReportingApi");
        try
        {
            var resp = await client.GetAsync($"api/reports/shipment/{id}", ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return StatusCode((int)resp.StatusCode, new Microsoft.AspNetCore.Mvc.ContentResult
            {
                Content = json,
                ContentType = "application/json"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Reporting API unavailable.", detail = ex.Message });
        }
    }
}
