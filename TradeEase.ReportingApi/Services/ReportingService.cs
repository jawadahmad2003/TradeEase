using Dapper;
using System.Data;
using TradeEase.ReportingApi.Models;

namespace TradeEase.ReportingApi.Services;

public interface IReportingService
{
    Task<IEnumerable<ShipmentReportModel>> GetShipmentReportAsync(
        DateTime? startDate, DateTime? endDate,
        string? shipmentType, string? status, CancellationToken ct = default);

    Task<IEnumerable<CountryAggregationModel>> GetTopCountriesAsync(int topN = 5, CancellationToken ct = default);

    Task<IEnumerable<ShipmentReportModel>> GetAllShipmentsAsync(int userId, CancellationToken ct = default);

    Task<ShipmentReportModel?> GetShipmentByIdAsync(int shipmentId, CancellationToken ct = default);
}

public class ReportingService : IReportingService
{
    private readonly IDbConnection _db;

    public ReportingService(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ShipmentReportModel>> GetShipmentReportAsync(
        DateTime? startDate, DateTime? endDate,
        string? shipmentType, string? status, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StartDate", startDate);
        parameters.Add("@EndDate", endDate);
        parameters.Add("@ShipmentType", shipmentType);
        parameters.Add("@Status", status);

        var cmd = new CommandDefinition(
            "dbo.sp_GenerateShipmentReport",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct);

        return await _db.QueryAsync<ShipmentReportModel>(cmd);
    }

    public async Task<IEnumerable<CountryAggregationModel>> GetTopCountriesAsync(int topN = 5, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@TopN", topN);

        var cmd = new CommandDefinition(
            "dbo.sp_GetTopImportExportCountries",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct);

        return await _db.QueryAsync<CountryAggregationModel>(cmd);
    }

    public async Task<IEnumerable<ShipmentReportModel>> GetAllShipmentsAsync(int userId, CancellationToken ct = default)
    {
        var sql = @"
            SELECT s.*, 
                   u.FullName AS UserFullName, u.Email AS UserEmail, u.BusinessName AS UserBusinessName,
                   p.Name AS PartyName, p.PartyType, p.Country AS PartyCountry,
                   h.HSCode, h.Description AS HSDescription, h.Chapter AS HSChapter,
                   h.CustomsDutyPct, h.SalesTaxPct
            FROM dbo.vw_ShipmentComprehensive s
            LEFT JOIN dbo.Users u ON u.UserId = @UserId
            LEFT JOIN dbo.TradeParties p ON p.PartyId = s.ShipmentId
            LEFT JOIN dbo.HSCodes h ON h.HSCode = s.HSCode
            WHERE @UserId = 0 OR s.UserEmail IN (SELECT Email FROM dbo.Users WHERE UserId = @UserId)
            ORDER BY s.CreatedAt DESC";

        var cmd = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct);
        return await _db.QueryAsync<ShipmentReportModel>(cmd);
    }

    public async Task<ShipmentReportModel?> GetShipmentByIdAsync(int shipmentId, CancellationToken ct = default)
    {
        var sql = "SELECT * FROM dbo.vw_ShipmentComprehensive WHERE ShipmentId = @ShipmentId";
        var cmd = new CommandDefinition(sql, new { ShipmentId = shipmentId }, cancellationToken: ct);
        return await _db.QueryFirstOrDefaultAsync<ShipmentReportModel>(cmd);
    }
}
