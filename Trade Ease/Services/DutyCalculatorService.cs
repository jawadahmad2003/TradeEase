using TradeEase.Models.Entities;

namespace TradeEase.Services;

public interface IDutyCalculatorService
{
    DutyBreakdown Calculate(decimal invoiceValueUsd, HSCode hsCode, decimal usdToPkr);
}

public record DutyBreakdown(
    decimal AssessedValuePkr,
    decimal RegulatoryDuty,
    decimal AdditionalCustomsDuty,
    decimal CustomsDuty,
    decimal SalesTax,
    decimal AIT,
    decimal TotalTax,
    decimal GrandTotal);

public class DutyCalculatorService : IDutyCalculatorService
{
    public DutyBreakdown Calculate(decimal invoiceValueUsd, HSCode hsCode, decimal usdToPkr)
    {
        var chapter = hsCode.Chapter ?? (hsCode.Code.Length >= 2 ? hsCode.Code[..2] : "00");
        var basePkr = invoiceValueUsd * usdToPkr;

        var rdPct = PakistanTradeReference.RegulatoryDutyPct(chapter);
        var acdPct = PakistanTradeReference.AdditionalCustomsDutyPct(chapter);
        var regulatory = basePkr * (rdPct / 100m);
        var additional = basePkr * (acdPct / 100m);

        var dutiable = basePkr + regulatory + additional;
        var customs = dutiable * (hsCode.CustomsDutyPct / 100m);
        var salesTax = (dutiable + customs) * (hsCode.SalesTaxPct / 100m);
        var ait = basePkr * (hsCode.AITPct / 100m);
        var totalTax = regulatory + additional + customs + salesTax + ait;

        return new DutyBreakdown(basePkr, regulatory, additional, customs, salesTax, ait, totalTax, basePkr + totalTax);
    }
}
