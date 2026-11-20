using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TradeEase.Services;

public interface ILiveForexService
{
    Task RefreshAsync(CancellationToken cancellationToken = default);
    LiveForexSnapshot GetSnapshot();
    decimal GetUsdToPkr(decimal fallback = 278.4m);
    decimal GetCurrencyToPkr(string currencyCode, decimal usdToPkr);
    IReadOnlyList<decimal> GetTrend(string currencyCode);
}

public record LiveForexRate(
    string CurrencyCode,
    decimal ExchangeRate,
    decimal? ChangePct,
    bool IsUp);

public record LiveForexSnapshot(
    IReadOnlyList<LiveForexRate> Rates,
    DateTime UpdatedUtc,
    string Source,
    int RefreshIntervalSeconds);

public class LiveForexService : ILiveForexService
{
    private static readonly string[] TargetCurrencies =
        new[]
        {
            "USD", "EUR", "GBP", "AED", "SAR", "CNY", "JPY", "SGD", "CHF", "CAD",
            "AUD", "INR", "TRY", "MYR", "THB", "KRW", "NZD", "QAR", "KWD", "BHD",
            "OMR", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BRL", "MXN",
            "ZAR", "IDR", "PHP", "VND", "HKD", "TWD", "RUB", "EGP", "NGN", "BDT",
        };

    /// <summary>Approximate units of currency per 1 USD — used when Frankfurter omits a code.</summary>
    private static readonly Dictionary<string, decimal> UnitsPerUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = 0.92m, ["GBP"] = 0.79m, ["AED"] = 3.67m, ["SAR"] = 3.75m, ["CNY"] = 7.24m,
        ["JPY"] = 150m, ["SGD"] = 1.34m, ["CHF"] = 0.88m, ["CAD"] = 1.36m, ["AUD"] = 1.52m,
        ["INR"] = 83.5m, ["TRY"] = 32m, ["MYR"] = 4.72m, ["THB"] = 35.5m, ["KRW"] = 1330m,
        ["NZD"] = 1.64m, ["QAR"] = 3.64m, ["KWD"] = 0.31m, ["BHD"] = 0.38m, ["OMR"] = 0.38m,
        ["SEK"] = 10.5m, ["NOK"] = 10.8m, ["DKK"] = 6.9m, ["PLN"] = 4.0m, ["CZK"] = 23m,
        ["HUF"] = 360m, ["RON"] = 4.6m, ["BRL"] = 5.0m, ["MXN"] = 17m, ["ZAR"] = 18.5m,
        ["IDR"] = 15800m, ["PHP"] = 56m, ["VND"] = 24500m, ["HKD"] = 7.82m, ["TWD"] = 32m,
        ["RUB"] = 92m, ["EGP"] = 48m, ["NGN"] = 1550m, ["BDT"] = 110m,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LiveForexService> _logger;
    private readonly object _lock = new();

    private LiveForexSnapshot _snapshot = new(Array.Empty<LiveForexRate>(), DateTime.MinValue, "frankfurter.app", 60);
    private readonly ConcurrentDictionary<string, decimal> _previousRates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<decimal>> _trends = new(StringComparer.OrdinalIgnoreCase);

    public LiveForexService(IHttpClientFactory httpClientFactory, ILogger<LiveForexService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public LiveForexSnapshot GetSnapshot() => _snapshot;

    public decimal GetUsdToPkr(decimal fallback = 282.5m)
    {
        var rate = _snapshot.Rates.FirstOrDefault(r => r.CurrencyCode == "USD");
        return rate?.ExchangeRate > 0 ? rate.ExchangeRate : fallback;
    }

    public decimal GetCurrencyToPkr(string currencyCode, decimal usdToPkr)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode == "PKR") return 1m;
        if (currencyCode == "USD") return usdToPkr;

        var rate = _snapshot.Rates.FirstOrDefault(r =>
            r.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
        return rate?.ExchangeRate > 0 ? rate.ExchangeRate : usdToPkr;
    }

    public IReadOnlyList<decimal> GetTrend(string currencyCode)
    {
        if (_trends.TryGetValue(currencyCode, out var trend) && trend.Count > 0)
            return trend;
        var current = _snapshot.Rates.FirstOrDefault(r =>
            r.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
        return current is null
            ? Array.Empty<decimal>()
            : new[] { current.ExchangeRate };
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("ForexApi");

        try
        {
            var response = await client.GetFromJsonAsync<ExchangeRateResponse>("latest/USD", cancellationToken);
            if (response?.Rates is null || !response.Rates.TryGetValue("PKR", out var usdToPkr) || usdToPkr <= 0)
                throw new InvalidOperationException("Invalid USD/PKR rate from open.er-api.com");

            var rates = new List<LiveForexRate> { BuildRate("USD", usdToPkr) };

            foreach (var code in TargetCurrencies.Where(c => c != "USD"))
            {
                if (response.Rates.TryGetValue(code, out var unitsPerUsd) && unitsPerUsd > 0)
                {
                    rates.Add(BuildRate(code, Math.Round(usdToPkr / unitsPerUsd, 4)));
                }
                else if (UnitsPerUsd.TryGetValue(code, out var fallbackPerUsd))
                {
                    rates.Add(BuildRate(code, Math.Round(usdToPkr / fallbackPerUsd, 4)));
                }
            }

            if (rates.Count < 5)
                throw new InvalidOperationException($"Too few forex rates returned ({rates.Count}).");

            lock (_lock)
            {
                _snapshot = new LiveForexSnapshot(
                    rates.OrderBy(r => r.CurrencyCode).ToList(),
                    DateTime.UtcNow,
                    $"Live forex (USD/PKR + {rates.Count - 1} pairs) · indicative for customs planning · not official SBP rate",
                    60);
            }

            _logger.LogInformation("Live forex refreshed: {Count} currencies", rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live forex refresh failed");
            if (_snapshot.Rates.Count == 0)
                ApplyFallbackSnapshot();
        }
    }

    private void ApplyFallbackSnapshot()
    {
        const decimal usdToPkr = 282.5m;
        var rates = new List<LiveForexRate> { BuildRate("USD", usdToPkr) };
        foreach (var code in TargetCurrencies.Where(c => c != "USD"))
        {
            if (UnitsPerUsd.TryGetValue(code, out var perUsd))
                rates.Add(BuildRate(code, Math.Round(usdToPkr / perUsd, 4)));
        }

        lock (_lock)
        {
            _snapshot = new LiveForexSnapshot(
                rates.OrderBy(r => r.CurrencyCode).ToList(),
                DateTime.UtcNow,
                "Indicative fallback rates (API unavailable, not stored in DB)",
                60);
        }
        _logger.LogWarning("Using fallback forex snapshot with {Count} currencies", rates.Count);
    }

    private LiveForexRate BuildRate(string code, decimal toPkr)
    {
        var trend = _trends.GetOrAdd(code, _ => SeedTrend(code, toPkr));
        
        decimal? changePct = null;
        var isUp = true;
        if (_previousRates.TryGetValue(code, out var prev) && prev > 0)
        {
            changePct = Math.Round((toPkr - prev) / prev * 100m, 3);
            isUp = changePct >= 0;
        }
        _previousRates[code] = toPkr;

        trend.Add(toPkr);
        while (trend.Count > 60) trend.RemoveAt(0);

        return new LiveForexRate(code, toPkr, changePct, isUp);
    }

    private List<decimal> SeedTrend(string code, decimal currentRate)
    {
        var list = new List<decimal>();
        var rand = new Random(code.GetHashCode());
        for (int i = 7; i >= 1; i--)
        {
            var percent = (decimal)(rand.NextDouble() * 0.03 - 0.015);
            list.Add(Math.Round(currentRate * (1 + percent), 4));
        }
        return list;
    }

    private sealed class ExchangeRateResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("base_code")]
        public string? BaseCode { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}
