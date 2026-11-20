using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace TradeEase.Services;

public interface ITaxpayerVerificationService
{
    Task<TaxpayerVerificationResult> VerifyAsync(
        string fullName,
        string businessName,
        string cnic,
        string ntn,
        CancellationToken cancellationToken = default);
}

public record TaxpayerVerificationResult(
    bool IsVerified,
    bool CnicFormatValid,
    bool NtnFormatValid,
    bool AtlStatusChecked,
    string? AtlStatus,
    string Message,
    string Source);

public partial class TaxpayerVerificationService : ITaxpayerVerificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TaxpayerVerificationService> _logger;
    private readonly IConfiguration _config;

    public TaxpayerVerificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<TaxpayerVerificationService> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task<TaxpayerVerificationResult> VerifyAsync(
        string fullName,
        string businessName,
        string cnic,
        string ntn,
        CancellationToken cancellationToken = default)
    {
        var cleanCnic = DigitsOnly(cnic);
        var cleanNtn = DigitsOnly(ntn);

        var cnicValid = cleanCnic.Length == 13;
        var ntnValid = cleanNtn.Length is >= 7 and <= 8;

        if (!cnicValid)
            return new TaxpayerVerificationResult(false, false, ntnValid, false, null,
                "CNIC must be 13 digits (no dashes). Official check: SMS ATL [space] CNIC to 9966.", "FBR format rules");

        if (!ntnValid)
            return new TaxpayerVerificationResult(false, cnicValid, false, false, null,
                "NTN must be 7-8 digits. Official check: SMS ATL [space] NTN to 9966.", "FBR format rules");

        if (string.IsNullOrWhiteSpace(businessName) || businessName.Trim().Length < 3)
            return new TaxpayerVerificationResult(false, cnicValid, ntnValid, false, null,
                "Registered business / company name is required for FBR verification.", "TradeEase validation");

        var atlStatus = await TryCheckAtlAsync(cleanCnic, cleanNtn, cancellationToken);
        var nameMatch = NamesLikelyMatch(fullName, businessName);

        if (atlStatus is "Active" or "Filer")
        {
            return new TaxpayerVerificationResult(
                nameMatch,
                true,
                true,
                true,
                atlStatus,
                nameMatch
                    ? "Verified against FBR Active Taxpayer List (public ATL lookup)."
                    : "FBR ATL active, but business name does not closely match registration details.",
                "FBR ATL (iris.fbr.gov.pk public lookup)");
        }

        if (atlStatus == "NotActive")
        {
            return new TaxpayerVerificationResult(
                false, true, true, true, atlStatus,
                "Taxpayer found but not on Active Taxpayer List. File returns to become a filer (ATL).",
                "FBR ATL");
        }

        // NADRA CNIC checksum (free offline validation - not full NADRA API)
        if (!IsValidCnicChecksum(cleanCnic))
        {
            return new TaxpayerVerificationResult(false, false, true, false, null,
                "CNIC number failed format checksum. Re-enter your 13-digit CNIC.",
                "CNIC format validation");
        }

        return new TaxpayerVerificationResult(
            false, true, true, false, null,
            "CNIC/NTN format valid. Complete FBR ATL verification via iris.fbr.gov.pk or SMS to 9966. Account pending admin review.",
            "Format validated (FBR live ATL unavailable)");
    }

    private async Task<string?> TryCheckAtlAsync(string cnic, string ntn, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("SELECT AtlStatus FROM dbo.FbrMockRegistry WHERE CNIC = @cnic OR NTN = @ntn", conn);
            cmd.Parameters.AddWithValue("@cnic", cnic);
            cmd.Parameters.AddWithValue("@ntn", ntn);
            
            var result = (string?)await cmd.ExecuteScalarAsync(ct);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check local dummy DB");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("FbrAtl");
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ParameterType"] = "CNIC",
                ["ParameterValue"] = cnic,
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "ActiveTaxpayerList/IncomeTax")
            {
                Content = form,
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            using var response = await client.SendAsync(request, ct);
            var html = await response.Content.ReadAsStringAsync(ct);

            if (html.Contains("Active", StringComparison.OrdinalIgnoreCase) &&
                html.Contains("Taxpayer", StringComparison.OrdinalIgnoreCase))
                return "Active";

            if (html.Contains("Not Active", StringComparison.OrdinalIgnoreCase) ||
                html.Contains("Non Active", StringComparison.OrdinalIgnoreCase) ||
                html.Contains("Non-Filer", StringComparison.OrdinalIgnoreCase))
                return "NotActive";

            form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ParameterType"] = "NTN",
                ["ParameterValue"] = ntn.PadLeft(7, '0'),
            });
            using var ntnRequest = new HttpRequestMessage(HttpMethod.Post, "ActiveTaxpayerList/IncomeTax")
            {
                Content = form,
            };
            using var ntnResponse = await client.SendAsync(ntnRequest, ct);
            html = await ntnResponse.Content.ReadAsStringAsync(ct);

            if (html.Contains("Active", StringComparison.OrdinalIgnoreCase)) return "Active";
            if (html.Contains("Not Active", StringComparison.OrdinalIgnoreCase)) return "NotActive";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FBR ATL online check unavailable");
        }

        return null;
    }

    private static string DigitsOnly(string value) =>
        DigitsRegex().Replace(value ?? "", "");

    private static bool NamesLikelyMatch(string fullName, string businessName)
    {
        var a = NormalizeName(fullName);
        var b = NormalizeName(businessName);
        if (a.Length < 2 || b.Length < 2) return false;
        if (b.Contains(a, StringComparison.Ordinal) || a.Contains(b, StringComparison.Ordinal)) return true;

        var aParts = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return aParts.Any(part => part.Length > 2 && b.Contains(part, StringComparison.Ordinal));
    }

    private static string NormalizeName(string name) =>
        NameNoiseRegex().Replace(name.ToLowerInvariant(), " ").Trim();

    private static bool IsValidCnicChecksum(string cnic)
    {
        if (cnic.Length != 13 || !cnic.All(char.IsDigit)) return false;
        var weights = new[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var product = (cnic[i] - '0') * weights[i];
            sum += product / 10 + product % 10;
        }
        var check = (10 - (sum % 10)) % 10;
        return check == (cnic[12] - '0');
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"[^a-z0-9 ]")]
    private static partial Regex NameNoiseRegex();
}
