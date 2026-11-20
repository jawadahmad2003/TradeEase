namespace TradeEase.Services;

using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;

public record TradeRouteContext(string ShipmentType, string OriginCountry, string DestinationCountry)
{
    public bool IsImportToPakistan =>
        ShipmentType.Equals("Import", StringComparison.OrdinalIgnoreCase) ||
        DestinationCountry.Equals("PK", StringComparison.OrdinalIgnoreCase);

    public bool IsExportFromPakistan =>
        ShipmentType.Equals("Export", StringComparison.OrdinalIgnoreCase) ||
        OriginCountry.Equals("PK", StringComparison.OrdinalIgnoreCase);

    public string RouteLabel =>
        $"{PakistanTradeReference.ResolveCountryLabel(OriginCountry)} → {PakistanTradeReference.ResolveCountryLabel(DestinationCountry)}";

    public static TradeRouteContext Normalize(string? shipmentType, string? origin, string? destination)
    {
        var type = string.IsNullOrWhiteSpace(shipmentType) ? "Import" : shipmentType.Trim();
        var o = (origin ?? "CN").Trim().ToUpperInvariant();
        var d = (destination ?? "PK").Trim().ToUpperInvariant();

        if (type.Equals("Export", StringComparison.OrdinalIgnoreCase))
        {
            o = "PK";
            if (d == "PK") d = "AE";
        }
        else
        {
            type = "Import";
            d = "PK";
            if (o == "PK") o = "CN";
        }

        return new TradeRouteContext(type, o, d);
    }
}

public record LiveCommodityTariff(
    string Code,
    string Description,
    decimal? ImportDutyPct,
    string Source,
    bool IsLive);

public interface ILiveTradeTariffService
{
    HSCodeDto MapProductFast(ProductSearchResult product, TradeRouteContext route);
    Task<HSCodeDto> EnrichProductAsync(ProductSearchResult product, TradeRouteContext route, CancellationToken ct = default);
    Task<HSCodeDto> ResolveByCodeAsync(string hsCode, TradeRouteContext route, CancellationToken ct = default);
    Task<DutyCalcResponse> CalculateForRouteAsync(
        HSCode hs,
        decimal invoiceValue,
        string currency,
        TradeRouteContext route,
        CancellationToken ct = default);
}

public class LiveTradeTariffService : ILiveTradeTariffService
{
    private readonly IUkHarmonizedTariffClient _ukTariff;
    private readonly IWorldHsCatalogService _catalog;
    private readonly IDutyCalculatorService _duty;
    private readonly ILiveForexService _forex;
    private readonly ILiveDataSyncState _sync;

    public LiveTradeTariffService(
        IUkHarmonizedTariffClient ukTariff,
        IWorldHsCatalogService catalog,
        IDutyCalculatorService duty,
        ILiveForexService forex,
        ILiveDataSyncState sync)
    {
        _ukTariff = ukTariff;
        _catalog = catalog;
        _duty = duty;
        _forex = forex;
        _sync = sync;
    }

    public HSCodeDto MapProductFast(ProductSearchResult product, TradeRouteContext route) =>
        BuildDto(product, null, route);

    public async Task<HSCodeDto> EnrichProductAsync(ProductSearchResult product, TradeRouteContext route, CancellationToken ct = default)
    {
        var live = await _ukTariff.GetCommodityAsync(product.Code, ct);
        return BuildDto(product, live, route);
    }

    public async Task<HSCodeDto> ResolveByCodeAsync(string hsCode, TradeRouteContext route, CancellationToken ct = default)
    {
        var catalog = _catalog.GetByCode(hsCode);
        if (catalog is null)
            return await BuildFromLiveOnlyAsync(hsCode, route, ct);

        return await EnrichProductAsync(catalog, route, ct);
    }

    public async Task<DutyCalcResponse> CalculateForRouteAsync(
        HSCode hs,
        decimal invoiceValue,
        string currency,
        TradeRouteContext route,
        CancellationToken ct = default)
    {
        var live = await _ukTariff.GetCommodityAsync(hs.Code, ct);
        var description = live?.Description ?? hs.Description;
        var hash = TradeProductHash.ComputeForRoute(hs.Code, description, route);
        var usdPkr = _forex.GetUsdToPkr();
        var valueUsd = ConvertToUsd(invoiceValue, currency, usdPkr);

        if (route.IsImportToPakistan)
        {
            var breakdown = _duty.Calculate(valueUsd, hs, usdPkr);
            _sync.MarkCatalogSearch(live?.IsLive == true ? live.Source : "FBR PCT route tariff");
            return new DutyCalcResponse(
                breakdown.CustomsDuty,
                breakdown.SalesTax,
                breakdown.AIT,
                breakdown.TotalTax,
                breakdown.AssessedValuePkr,
                usdPkr,
                hash,
                hs.SalesTaxPct,
                breakdown.GrandTotal,
                breakdown.RegulatoryDuty,
                breakdown.AdditionalCustomsDuty,
                "FBR Pakistan import (CD + RD + ST + AIT)",
                live?.IsLive == true ? $"{live.Source} + FBR chapter rates" : "FBR PCT FY 2025-26",
                "PK",
                route.OriginCountry,
                route.DestinationCountry,
                route.ShipmentType,
                route.RouteLabel,
                null,
                live?.IsLive ?? false);
        }

        var destDuty = await ResolveDestinationDutyPctAsync(hs.Code, hs.Chapter, route.DestinationCountry, live, ct);
        var destDutyAmount = valueUsd * usdPkr * (destDuty / 100m);
        _sync.MarkCatalogSearch(live?.IsLive == true ? live.Source : "export route tariff");

        return new DutyCalcResponse(
            0,
            0,
            0,
            destDutyAmount,
            valueUsd * usdPkr,
            usdPkr,
            hash,
            0,
            valueUsd * usdPkr + destDutyAmount,
            0,
            0,
            $"Export from PK · destination import duty ({route.DestinationCountry})",
            live?.Source ?? "Harmonized schedule",
            route.DestinationCountry,
            route.OriginCountry,
            route.DestinationCountry,
            route.ShipmentType,
            route.RouteLabel,
            destDuty,
            live?.IsLive ?? false);
    }

    private async Task<HSCodeDto> BuildFromLiveOnlyAsync(string hsCode, TradeRouteContext route, CancellationToken ct)
    {
        var live = await _ukTariff.GetCommodityAsync(hsCode, ct);
        if (live is null)
            throw new InvalidOperationException($"HS code {hsCode} not found in live catalog.");

        var chapter = hsCode.Length >= 2 ? hsCode[..2] : "00";
        var (customs, sales, ait) = WorldHsCatalogBuilder.DutyForChapter(chapter);
        var unit = WorldHsCatalogBuilder.UnitForChapter(chapter);
        var product = new ProductSearchResult(
            live.Code,
            live.Description,
            chapter,
            unit,
            customs,
            sales,
            ait,
            "FY 2025-26",
            TradeProductHash.ComputeForRoute(live.Code, live.Description, route),
            live.IsLive);

        return BuildDto(product, live, route);
    }

    private HSCodeDto BuildDto(ProductSearchResult product, LiveCommodityTariff? live, TradeRouteContext route)
    {
        var description = live?.Description ?? product.Description;
        var hash = TradeProductHash.ComputeForRoute(product.Code, description, route);

        if (route.IsImportToPakistan)
        {
            return new HSCodeDto(
                0,
                product.Code,
                description,
                product.Unit,
                product.CustomsDutyPct,
                product.SalesTaxPct,
                product.AITPct,
                live?.IsLive == true
                    ? $"FBR PCT · live HS from {live.Source}"
                    : product.TariffYear,
                hash,
                route.OriginCountry,
                route.DestinationCountry,
                null,
                "FBR Pakistan import tariff",
                live?.IsLive == true ? live.Source : "FBR PCT + embedded catalog",
                live?.IsLive ?? product.IsLiveApi);
        }

        var destDuty = ResolveDestinationDutyPct(product.Code, product.Chapter, route.DestinationCountry, live);
        return new HSCodeDto(
            0,
            product.Code,
            description,
            product.Unit,
            destDuty,
            0,
            0,
            live?.IsLive == true
                ? $"Export · live destination duty ref · {live.Source}"
                : $"Export · destination duty ({route.DestinationCountry})",
            hash,
            route.OriginCountry,
            route.DestinationCountry,
            destDuty,
            $"Export to {PakistanTradeReference.ResolveCountryLabel(route.DestinationCountry)}",
            live?.Source ?? "Harmonized + destination schedule",
            live?.IsLive ?? product.IsLiveApi);
    }

    private async Task<decimal> ResolveDestinationDutyPctAsync(
        string hsCode,
        string? chapter,
        string destinationCountry,
        LiveCommodityTariff? live,
        CancellationToken ct)
    {
        if (live is null)
            live = await _ukTariff.GetCommodityAsync(hsCode, ct);

        var ch = chapter ?? (hsCode.Length >= 2 ? hsCode[..2] : "00");
        return PakistanTradeReference.DestinationImportDutyPct(
            destinationCountry,
            ch,
            live?.ImportDutyPct);
    }

    private static decimal ResolveDestinationDutyPct(
        string hsCode,
        string? chapter,
        string destinationCountry,
        LiveCommodityTariff? live)
    {
        var ch = chapter ?? (hsCode.Length >= 2 ? hsCode[..2] : "00");
        return PakistanTradeReference.DestinationImportDutyPct(destinationCountry, ch, live?.ImportDutyPct);
    }

    private decimal ConvertToUsd(decimal invoiceValue, string currency, decimal usdPkr)
    {
        if (currency.Equals("USD", StringComparison.OrdinalIgnoreCase)) return invoiceValue;
        if (currency.Equals("PKR", StringComparison.OrdinalIgnoreCase)) return invoiceValue / usdPkr;
        var curPkr = _forex.GetCurrencyToPkr(currency, usdPkr);
        if (curPkr <= 0) return invoiceValue;
        return (invoiceValue * curPkr) / usdPkr;
    }
}
