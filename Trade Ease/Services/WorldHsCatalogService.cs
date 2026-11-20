using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Entities;

namespace TradeEase.Services;

public interface IWorldHsCatalogService
{
    int TotalProducts { get; }
    DateTime? LastRefreshUtc { get; }
    string CatalogSource { get; }
    IReadOnlyList<ProductSearchResult> Search(string? query, int limit = 25);
    Task<IReadOnlyList<ProductSearchResult>> SearchLiveAsync(string? query, int limit = 25, CancellationToken ct = default);
    ProductSearchResult? GetByCode(string code);
    Task RefreshFromExternalApisAsync(CancellationToken ct = default);
    Task SyncToDatabaseAsync(TradeEaseDbContext db, CancellationToken ct = default);
}

public record ProductSearchResult(
    string Code,
    string Description,
    string Chapter,
    string Unit,
    decimal CustomsDutyPct,
    decimal SalesTaxPct,
    decimal AITPct,
    string TariffYear,
    string ProductHash,
    bool IsLiveApi = false);

public class WorldHsCatalogService : IWorldHsCatalogService
{
    private readonly List<CatalogProduct> _catalog;
    private readonly IUkHarmonizedTariffClient _ukTariff;
    private readonly ILiveDataSyncState _sync;
    private readonly IConfiguration _config;
    private readonly ILogger<WorldHsCatalogService> _logger;
    private readonly object _lock = new();
    private DateTime? _lastRefreshUtc;
    private string _catalogSource = "embedded harmonized catalog (HS chapters 01-97 sample)";
    private readonly int _embeddedCount;
    private readonly int _maxInMemory;
    private readonly bool _cacheLiveSearch;

    public WorldHsCatalogService(
        IUkHarmonizedTariffClient ukTariff,
        ILiveDataSyncState sync,
        IConfiguration config,
        ILogger<WorldHsCatalogService> logger)
    {
        _ukTariff = ukTariff;
        _sync = sync;
        _config = config;
        _logger = logger;
        _maxInMemory = Math.Clamp(config.GetValue("LiveData:MaxInMemoryCatalog", 800), 600, 2000);
        _cacheLiveSearch = config.GetValue("LiveData:CacheLiveSearchResults", false);
        _catalog = WorldHsCatalogBuilder.Build().ToList();
        _embeddedCount = _catalog.Count;
        _logger.LogInformation(
            "World HS catalog bootstrapped: {Count} embedded products (live search on demand, max {Max} in memory)",
            _catalog.Count,
            _maxInMemory);
    }

    public int TotalProducts
    {
        get { lock (_lock) return _catalog.Count; }
    }

    public DateTime? LastRefreshUtc
    {
        get { lock (_lock) return _lastRefreshUtc; }
    }

    public string CatalogSource
    {
        get { lock (_lock) return _catalogSource; }
    }

    public IReadOnlyList<ProductSearchResult> Search(string? query, int limit = 25) =>
        SearchInternal(query, limit);

    public async Task<IReadOnlyList<ProductSearchResult>> SearchLiveAsync(
        string? query, int limit = 25, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return SearchInternal(null, Math.Min(limit, 25));

        var term = query.Trim();
        var merged = new Dictionary<string, ProductSearchResult>(StringComparer.OrdinalIgnoreCase);

        if (term.All(char.IsDigit) && term.Length >= 4)
        {
            var commodity = await _ukTariff.GetCommodityAsync(term, ct);
            if (commodity is not null)
            {
                if (_cacheLiveSearch)
                {
                    lock (_lock) { UpsertFromLiveCommodityLocked(commodity); }
                }

                merged[commodity.Code] = ToSearchResult(commodity.Code, commodity.Description, true);
            }
        }

        var liveHits = await _ukTariff.SearchAsync(term, ct);
        if (_cacheLiveSearch)
        {
            lock (_lock)
            {
                foreach (var (code, description) in liveHits)
                    UpsertLocked(code, description, fromApi: true);
            }
        }

        foreach (var (code, description) in liveHits)
            merged[code] = ToSearchResult(code, description, true);

        if (merged.Count > 0)
            _sync.MarkCatalogSearch("UK Trade Tariff API (live search)");

        foreach (var local in SearchInternal(query, limit))
        {
            if (!merged.ContainsKey(local.Code))
                merged[local.Code] = local;
        }

        return merged.Values
            .OrderByDescending(p => p.IsLiveApi)
            .ThenByDescending(p => ScoreProductTerm(p, term))
            .ThenBy(p => p.Code)
            .Take(limit)
            .ToList();
    }

    public ProductSearchResult? GetByCode(string code)
    {
        lock (_lock)
        {
            var match = _catalog.FirstOrDefault(p =>
                p.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
            return match is null ? null : MapProduct(match, match.TariffYear.Contains("live", StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task RefreshFromExternalApisAsync(CancellationToken ct = default)
    {
        var added = 0;
        var refreshAllChapters = _config.GetValue("LiveData:RefreshAllHsChapters", true);

        if (refreshAllChapters)
        {
            for (var ch = 1; ch <= 97; ch++)
            {
                if (ct.IsCancellationRequested) break;
                var chapter = ch.ToString("D2");
                var before = TotalProducts;
                try
                {
                    var items = await _ukTariff.FetchChapterAsync(chapter, ct);
                    lock (_lock)
                    {
                        foreach (var (code, description) in items)
                            UpsertLocked(code, description, fromApi: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Chapter {Chapter} live refresh skipped", chapter);
                }

                if (TotalProducts > before) added += TotalProducts - before;
                await Task.Delay(120, ct);
            }
        }

        var extraQueries = _config.GetSection("LiveData:CatalogSeedQueries").Get<string[]>() ?? Array.Empty<string>();
        foreach (var q in extraQueries)
        {
            if (ct.IsCancellationRequested) break;
            var before = TotalProducts;
            await MergeLiveSearchAsync(q, ct);
            if (TotalProducts > before) added += TotalProducts - before;
            await Task.Delay(100, ct);
        }

        lock (_lock)
        {
            _lastRefreshUtc = DateTime.UtcNow;
            _catalogSource = refreshAllChapters
                ? $"UK Trade Tariff API · all HS chapters 01–97 ({_catalog.Count} products, auto-renewed)"
                : $"UK Trade Tariff API + catalog ({_catalog.Count} products)";
        }

        _sync.MarkCatalogRefreshed(_catalogSource, TotalProducts);
        _logger.LogInformation("Catalog refresh complete: {Total} products ({Added} new from live API)", TotalProducts, added);
    }

    public async Task SyncToDatabaseAsync(TradeEaseDbContext db, CancellationToken ct = default)
    {
        List<CatalogProduct> snapshot;
        lock (_lock) snapshot = _catalog.Take(_embeddedCount).ToList();

        var existingSet = await db.HSCodes
            .AsNoTracking()
            .Select(h => h.Code)
            .ToHashSetAsync(ct);

        var added = 0;
        foreach (var p in snapshot)
        {
            if (existingSet.Contains(p.Code)) continue;
            db.HSCodes.Add(new HSCode
            {
                Code = p.Code,
                Description = p.Description,
                Chapter = p.Chapter,
                UnitOfMeasure = p.Unit,
                CustomsDutyPct = p.CustomsDutyPct,
                SalesTaxPct = p.SalesTaxPct,
                AITPct = p.AITPct,
                TariffYear = p.TariffYear,
                IsActive = true,
            });
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);
    }

    private async Task MergeLiveSearchAsync(string query, CancellationToken ct)
    {
        var term = query.Trim();
        if (term.All(char.IsDigit) && term.Length >= 4)
        {
            var commodity = await _ukTariff.GetCommodityAsync(term, ct);
            if (commodity is not null)
            {
                lock (_lock) { UpsertFromLiveCommodityLocked(commodity); }
                return;
            }
        }

        var live = await _ukTariff.SearchAsync(term, ct);
        if (live.Count == 0) return;

        lock (_lock)
        {
            foreach (var (code, description) in live)
                UpsertLocked(code, description, fromApi: true);
        }
    }

    private void UpsertFromLiveCommodityLocked(LiveCommodityTariff commodity) =>
        UpsertLocked(commodity.Code, commodity.Description, fromApi: true);

    private void UpsertLocked(string code, string description, bool fromApi)
    {
        var chapter = code.Length >= 2 ? code[..2] : "00";
        var (customs, sales, ait) = WorldHsCatalogBuilder.DutyForChapter(chapter);
        var unit = WorldHsCatalogBuilder.UnitForChapter(chapter);
        var tariffYear = fromApi
            ? "FY 2025-26 · live harmonized (UK Trade Tariff API)"
            : "FY 2025-26";

        var product = new CatalogProduct(code, description, chapter, unit, customs, sales, ait, tariffYear);
        var idx = _catalog.FindIndex(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            _catalog[idx] = product;
            return;
        }

        _catalog.Add(product);
        if (_catalog.Count > _maxInMemory)
            TrimLiveEntriesLocked();
    }

    private void TrimLiveEntriesLocked()
    {
        var liveEntries = _catalog
            .Skip(_embeddedCount)
            .OrderBy(p => p.Code)
            .Take(Math.Max(0, _catalog.Count - _maxInMemory))
            .ToList();

        foreach (var entry in liveEntries)
            _catalog.Remove(entry);
    }

    private IReadOnlyList<ProductSearchResult> SearchInternal(string? query, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);
        lock (_lock)
        {
            IEnumerable<CatalogProduct> source = _catalog;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                source = _catalog
                    .Select(p => (p, ScoreProduct(p, term, tokens)))
                    .Where(x => x.Item2 > 0)
                    .OrderByDescending(x => x.Item2)
                    .ThenBy(x => x.Item1.Code)
                    .Select(x => x.Item1);
            }
            else
            {
                source = _catalog.OrderBy(p => p.Code);
            }

            return source
                .Take(limit)
                .Select(p => MapProduct(p, p.TariffYear.Contains("live", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    private ProductSearchResult ToSearchResult(string code, string description, bool isLive)
    {
        var chapter = code.Length >= 2 ? code[..2] : "00";
        var (customs, sales, ait) = WorldHsCatalogBuilder.DutyForChapter(chapter);
        var unit = WorldHsCatalogBuilder.UnitForChapter(chapter);
        return new ProductSearchResult(
            code,
            description,
            chapter,
            unit,
            customs,
            sales,
            ait,
            isLive ? "FY 2025-26 · live harmonized (UK Trade Tariff API)" : "FY 2025-26",
            TradeProductHash.Compute(code, description),
            isLive);
    }

    private static ProductSearchResult MapProduct(CatalogProduct p, bool isLive) => new(
        p.Code,
        p.Description,
        p.Chapter,
        p.Unit,
        p.CustomsDutyPct,
        p.SalesTaxPct,
        p.AITPct,
        p.TariffYear,
        TradeProductHash.Compute(p.Code, p.Description),
        isLive);

    private static int ScoreProductTerm(ProductSearchResult p, string term)
    {
        var score = 0;
        if (p.Code.StartsWith(term, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (p.Code.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 60;
        if (p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 40;
        return score;
    }

    private static int ScoreProduct(CatalogProduct p, string term, string[] tokens)
    {
        var score = 0;
        if (p.Code.StartsWith(term, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (p.Code.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 60;
        if (p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 40;

        foreach (var token in tokens)
        {
            if (p.Code.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 20;
            if (p.Description.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 15;
        }
        return score;
    }
}
