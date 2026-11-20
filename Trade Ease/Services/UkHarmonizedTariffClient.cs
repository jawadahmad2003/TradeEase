using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TradeEase.Services;

public interface IUkHarmonizedTariffClient
{
    Task<IReadOnlyList<(string Code, string Description)>> SearchAsync(string query, CancellationToken ct = default);
    Task<LiveCommodityTariff?> GetCommodityAsync(string hsCode, CancellationToken ct = default);
    Task<IReadOnlyList<(string Code, string Description)>> FetchChapterAsync(string chapterTwoDigits, CancellationToken ct = default);
}

public class UkHarmonizedTariffClient : IUkHarmonizedTariffClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UkHarmonizedTariffClient> _logger;

    public UkHarmonizedTariffClient(IHttpClientFactory httpClientFactory, ILogger<UkHarmonizedTariffClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(string Code, string Description)>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return Array.Empty<(string Code, string Description)>();

        var client = _httpClientFactory.CreateClient("UkTariff");
        var q = Uri.EscapeDataString(query.Trim());
        var paths = new[]
        {
            $"api/v2/commodities/search?query={q}",
            $"api/v2/search_references?query={q}",
            $"uk/api/search?q={q}",
        };

        foreach (var path in paths)
        {
            try
            {
                using var doc = await client.GetFromJsonAsync<JsonDocument>(path, ct);
                if (doc is null) continue;
                var parsed = ParseSearchResults(doc.RootElement);
                if (parsed.Count > 0) return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "UK tariff search failed for path {Path}", path);
            }
        }

        return Array.Empty<(string Code, string Description)>();
    }

    public async Task<IReadOnlyList<(string Code, string Description)>> FetchChapterAsync(
        string chapterTwoDigits, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterTwoDigits)) return Array.Empty<(string Code, string Description)>();
        var ch = chapterTwoDigits.Trim().PadLeft(2, '0');
        if (!int.TryParse(ch, out var num) || num is < 1 or > 97) return Array.Empty<(string Code, string Description)>();

        var client = _httpClientFactory.CreateClient("UkTariff");
        var paths = new[]
        {
            $"api/v2/chapters/{ch}",
            $"api/v2/headings?filter[chapter]={ch}",
            $"api/v2/search_references?query=chapter%20{ch}",
        };

        foreach (var path in paths)
        {
            try
            {
                using var doc = await client.GetFromJsonAsync<JsonDocument>(path, ct);
                if (doc is null) continue;
                var parsed = ParseSearchResults(doc.RootElement);
                if (parsed.Count > 0) return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "UK chapter fetch failed for {Chapter} path {Path}", ch, path);
            }
        }

        return Array.Empty<(string Code, string Description)>();
    }

    public async Task<LiveCommodityTariff?> GetCommodityAsync(string hsCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hsCode)) return null;
        var client = _httpClientFactory.CreateClient("UkTariff");
        var normalized = NormalizeHsCode(hsCode);
        var commodityId = normalized.PadRight(10, '0');
        var paths = new[]
        {
            $"api/v2/commodities/{commodityId}",
            $"uk/api/commodities/{normalized}",
        };

        foreach (var path in paths)
        {
            try
            {
                using var doc = await client.GetFromJsonAsync<JsonDocument>(path, ct);
                if (doc is null) continue;
                var parsed = ParseCommodity(doc.RootElement, normalized);
                if (parsed is not null) return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "UK commodity fetch failed for {Path}", path);
            }
        }

        return null;
    }

    private static LiveCommodityTariff? ParseCommodity(JsonElement root, string code)
    {
        string? description = null;
        decimal? duty = null;

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            if (data.TryGetProperty("attributes", out var attrs))
            {
                description = attrs.TryGetProperty("description", out var d) ? d.GetString()
                    : attrs.TryGetProperty("formatted_description", out var fd) ? fd.GetString() : null;
            }
        }

        if (root.TryGetProperty("included", out var included) && included.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in included.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var typeProp)) continue;
                var type = typeProp.GetString()?.ToLowerInvariant();
                if (type is not ("measure" or "measure_component")) continue;
                if (!item.TryGetProperty("attributes", out var mAttrs)) continue;

                var expr = mAttrs.TryGetProperty("duty_expression", out var de) ? de.GetString()
                    : mAttrs.TryGetProperty("import_duty_expression", out var ide) ? ide.GetString()
                    : mAttrs.TryGetProperty("formatted_duty_expression", out var fde) ? fde.GetString() : null;

                var parsed = ParseDutyExpression(expr);
                if (parsed is not null)
                {
                    duty = parsed;
                    break;
                }
            }
        }

        WalkJsonForDuty(root, ref duty, ref description, depth: 0);

        if (string.IsNullOrWhiteSpace(description)) return null;

        return new LiveCommodityTariff(
            code,
            description.Trim(),
            duty,
            "UK Trade Tariff API (live)",
            true);
    }

    private static void WalkJsonForDuty(JsonElement element, ref decimal? duty, ref string? description, int depth)
    {
        if (depth > 10) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var name = prop.Name.ToLowerInvariant();
                if (description is null && name is "description" or "formatted_description")
                {
                    var val = prop.Value.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(val) && val.Length > 3) description = val;
                }

                if (duty is null && name.Contains("duty") && prop.Value.ValueKind == JsonValueKind.String)
                {
                    duty = ParseDutyExpression(prop.Value.GetString());
                }

                WalkJsonForDuty(prop.Value, ref duty, ref description, depth + 1);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                WalkJsonForDuty(item, ref duty, ref description, depth + 1);
        }
    }

    private static decimal? ParseDutyExpression(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        var match = Regex.Match(expression, @"(\d+(?:\.\d+)?)\s*%");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var pct))
            return pct;
        if (expression.Contains("Free", StringComparison.OrdinalIgnoreCase)) return 0m;
        return null;
    }

    private static List<(string Code, string Description)> ParseSearchResults(JsonElement root)
    {
        var results = new List<(string, string)>();
        WalkJson(root, results, depth: 0);
        return results
            .GroupBy(r => r.Item1, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(250)
            .ToList();
    }

    private static void WalkJson(JsonElement element, List<(string Code, string Description)> results, int depth)
    {
        if (depth > 8 || results.Count >= 50) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            string? code = null;
            string? desc = null;

            foreach (var prop in element.EnumerateObject())
            {
                var name = prop.Name.ToLowerInvariant();
                if (code is null && (name is "goods_nomenclature_item_id" or "commodity_code" or "code" or "id"))
                {
                    var raw = prop.Value.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(raw) && raw.Any(char.IsDigit))
                        code = NormalizeHsCode(raw);
                }
                if (desc is null && (name is "description" or "description_plain" or "formatted_description" or "title"))
                {
                    var raw = prop.Value.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(raw) && raw.Length > 3)
                        desc = raw;
                }
            }

            if (code is not null && desc is not null && code.Length >= 6)
                results.Add((code, desc));

            foreach (var prop in element.EnumerateObject())
                WalkJson(prop.Value, results, depth + 1);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                WalkJson(item, results, depth + 1);
        }
    }

    private static string NormalizeHsCode(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length >= 8) return digits[..8];
        if (digits.Length >= 6) return digits.PadRight(8, '0');
        return digits;
    }
}
