namespace TradeEase.Services;

/// <summary>Pakistan import/export defaults and sorting helpers for Pakistani traders.</summary>
public static class PakistanTradeReference
{
    public static readonly string[] PinnedCountryCodes =
        new[] { "PK", "CN", "AE", "SA", "US", "GB", "DE", "IN", "AF", "IR" };

    public static readonly string[] TraderCurrencies =
        new[] { "USD", "PKR", "AED", "CNY", "SAR", "EUR", "GBP" };

    public static readonly string[] PriorityForexCodes =
        new[] { "USD", "AED", "CNY", "SAR", "EUR", "GBP" };

    private static readonly HashSet<string> GccCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AE", "SA", "QA", "KW", "BH", "OM",
    };

    private static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE", "IT",
        "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
    };

    public static IEnumerable<T> SortCountries<T>(IEnumerable<T> countries, Func<T, string> codeSelector) =>
        countries
            .OrderBy(c =>
            {
                var code = codeSelector(c);
                var idx = Array.IndexOf(PinnedCountryCodes, code.ToUpperInvariant());
                return idx >= 0 ? idx : 1000;
            })
            .ThenBy(c => codeSelector(c));

    public static string FormatRoute(string? originCode, string? destCode)
    {
        var origin = ResolveCountryLabel(originCode);
        var dest = ResolveCountryLabel(destCode);
        if (origin == "—" && dest == "—") return "—";
        return $"{origin} → {dest}";
    }

    public static string ResolveCountryLabel(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "—";
        var trimmed = code.Trim();
        var fromEmbedded = WorldGeoData.TryGetCountryName(trimmed);
        if (fromEmbedded is not null) return fromEmbedded;
        return trimmed.Length == 2 ? trimmed.ToUpperInvariant() : trimmed;
    }

    public static decimal RegulatoryDutyPct(string? chapter) => chapter switch
    {
        "87" => 50m,
        "84" or "85" => 15m,
        "52" or "61" or "62" or "63" => 10m,
        "27" => 20m,
        "24" => 100m,
        "22" => 50m,
        "17" => 25m,
        "10" or "11" => 5m,
        _ => 0m,
    };

    public static decimal AdditionalCustomsDutyPct(string? chapter) => chapter switch
    {
        "87" => 7m,
        "84" or "85" => 4m,
        "72" or "73" => 3m,
        _ => 0m,
    };

    /// <summary>Destination import duty for any ISO country — uses live MFN when available.</summary>
    public static decimal DestinationImportDutyPct(string destinationCode, string chapter, decimal? liveMfnPct = null)
    {
        var dest = destinationCode.ToUpperInvariant();
        var factor = DestinationFactor(dest);

        if (liveMfnPct is decimal mfn)
            return Math.Round(Math.Max(mfn * factor, 0), 2);

        return Math.Round(ChapterDestinationBase(chapter) * factor, 2);
    }

    public static decimal DestinationFactor(string destinationCode)
    {
        var dest = destinationCode.ToUpperInvariant();
        
        // Top 5 Trading Partners - Explicit Hardcoded Factors
        if (dest == "US") return 1.15m; // USA
        if (dest == "GB") return 1.00m; // UK
        if (dest == "CN") return 1.10m; // China
        if (dest == "AE") return 0.85m; // UAE
        if (dest == "SA") return 0.85m; // Saudi Arabia

        // Other regions/countries
        if (GccCountries.Contains(dest)) return 0.85m;
        if (EuCountries.Contains(dest)) return 1.00m;
        if (dest is "CA") return 1.15m;
        if (dest is "HK" or "TW") return 1.10m;
        if (dest is "IN" or "BD" or "LK" or "NP") return 1.05m;
        if (dest is "JP" or "KR" or "SG" or "MY" or "TH" or "VN" or "ID" or "PH") return 1.08m;
        if (dest is "AU" or "NZ") return 1.10m;
        if (dest is "BR" or "MX" or "AR" or "CL") return 1.12m;
        if (dest is "ZA" or "NG" or "KE" or "EG") return 0.95m;
        if (dest is "TR" or "RU") return 1.05m;
        return 1.00m;
    }

    private static decimal ChapterDestinationBase(string chapter) => chapter switch
    {
        "87" => 25m,
        "84" or "85" => 5m,
        "52" or "61" or "62" => 12m,
        "27" => 5m,
        "30" => 0m,
        "04" or "07" or "08" => 8m,
        _ => 6m,
    };
}
