namespace TradeEase.Models.Dtos;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string FullName, string Email, string BusinessName, string Cnic, string Ntn, string Password);
public record VerifyTaxpayerRequest(string FullName, string BusinessName, string Cnic, string Ntn);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record TaxpayerVerificationDto(
    bool IsVerified,
    bool CnicFormatValid,
    bool NtnFormatValid,
    bool AtlStatusChecked,
    string? AtlStatus,
    string Message,
    string Source);

public record AuthResponse(
    string Token,
    int UserId,
    string FullName,
    string Email,
    string Role,
    bool IsVerified,
    string? VerificationMessage = null,
    bool RequiresTwoFactor = false,
    string? TwoFactorToken = null);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record CompleteTwoFactorRequest(string TwoFactorToken, string Code);
public record EnableTwoFactorRequest(string Secret, string Code);
public record DisableTwoFactorRequest(string Code, string Password);
public record TwoFactorSetupResponse(string Secret, string OtpAuthUri, string QrCodeBase64);

public record DashboardDto(
    int TotalShipments,
    int PendingShipments,
    int ClearedShipments,
    decimal TotalTradeValue,
    IReadOnlyList<ShipmentSummaryDto> RecentShipments,
    IReadOnlyList<ForexRateDto> ForexRates,
    IReadOnlyList<TopTradePartnerDto> TopTradePartners,
    int ImportShipments = 0,
    int ExportShipments = 0);

public record ShipmentSummaryDto(
    int ShipmentId,
    string Reference,
    string ProductDesc,
    string Status,
    decimal? TotalValuePKR,
    DateTime CreatedAt,
    string? ShipmentType = null,
    string? Route = null);

public record TopTradePartnerDto(
    int PartyId,
    string Name,
    string PartyType,
    string? Country,
    int ShipmentCount,
    decimal? TradeValuePkr);

public record ShipmentListDto(
    int ShipmentId,
    string Reference,
    string Type,
    string? HsCode,
    string ProductDesc,
    string? SubDesc,
    string Route,
    string? Etd,
    string? Eta,
    string Value,
    string Status,
    DateTime CreatedAt);

public record ShipmentDetailDto(
    int ShipmentId,
    string Reference,
    string Type,
    string ProductDesc,
    string Status,
    decimal InvoiceValue,
    string Currency,
    decimal? TotalValuePKR,
    string? OriginPort,
    string? DestinationPort,
    string? DestinationCountry,
    string? HsCode,
    string? PartyName,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    IReadOnlyList<ShipmentDocumentDto> Documents);

public record StatusHistoryDto(string Status, DateTime ChangedAt, string? Notes);

public record TradePartyDto(
    int PartyId,
    string PartyType,
    string Name,
    string? City,
    string? Country,
    string? Email,
    string? Phone,
    string? TaxId,
    bool IsActive);

public record CreateTradePartyRequest(
    string PartyType,
    string Name,
    string? City,
    string? Country,
    string? Address,
    string? Email,
    string? Phone,
    string? TaxId);

public record HSCodeDto(
    int HsCodeId,
    string Code,
    string Description,
    string? UnitOfMeasure,
    decimal CustomsDutyPct,
    decimal SalesTaxPct,
    decimal AITPct,
    string TariffYear,
    string ProductHash,
    string CountryOfOrigin = "PK",
    string? DestinationCountryCode = null,
    decimal? DestinationDutyPct = null,
    string? TariffRegime = null,
    string? TariffSource = null,
    bool LiveApi = false);

public record ProductSearchResponse(
    int Total,
    IReadOnlyList<HSCodeDto> Items,
    string TariffYear,
    DateTime UpdatedAt,
    string Source,
    bool LiveApiMerged = false,
    string? TradeRoute = null,
    string? TariffRegime = null,
    string? ShipmentType = null,
    string? OriginCountry = null,
    string? DestinationCountry = null);

public record LiveFeedStatusDto(
    string Name,
    DateTime? LastSuccessUtc,
    string Source,
    int? ItemCount,
    int RefreshIntervalSeconds,
    string? LastError);

public record LiveStatusDto(
    DateTime ServerUtc,
    IReadOnlyList<LiveFeedStatusDto> Feeds,
    DateTime ForexUpdatedUtc,
    string ForexSource,
    int ForexCurrencyCount,
    DateTime? GeoUpdatedUtc,
    string GeoSource,
    int CountryCount,
    DateTime? CatalogUpdatedUtc,
    string CatalogSource,
    int ProductCount);

public record HSCodeSearchResponse(
    int Total,
    IReadOnlyList<HSCodeDto> Items,
    string TariffYear,
    DateTime UpdatedAt);

public record DutyCalcRequest(
    string HsCode,
    decimal InvoiceValue,
    string Currency,
    string? ShipmentType = null,
    string? OriginCountry = null,
    string? DestinationCountry = null);

public record DutyCalcResponse(
    decimal CustomsDuty,
    decimal SalesTax,
    decimal Ait,
    decimal TotalDuty,
    decimal TotalValuePKR,
    decimal ExchangeRate,
    string ProductHash,
    decimal VatPct,
    decimal TotalLandedCostPkr,
    decimal RegulatoryDuty = 0,
    decimal AdditionalCustomsDuty = 0,
    string? TariffRegime = null,
    string? TariffSource = null,
    string? AppliedCountry = null,
    string? OriginCountry = null,
    string? DestinationCountry = null,
    string? ShipmentType = null,
    string? TradeRoute = null,
    decimal? DestinationDutyPct = null,
    bool LiveApi = false);

public record ForexRateDto(
    string CurrencyCode,
    decimal ExchangeRate,
    string RateDate,
    decimal? ChangePct = null,
    bool? IsUp = null);

public record PortDetailDto(string Name, string Type, string? City = null);

public record CountryDetailDto(
    string Code,
    string Name,
    string Region,
    IReadOnlyList<PortDetailDto> Ports);

public record ForexLiveResponseDto(
    IReadOnlyList<ForexRateDto> Rates,
    DateTime LastUpdatedUtc,
    string Source,
    int RefreshIntervalSeconds);

public record NotificationDto(
    string Id,
    string Text,
    DateTime OccurredAt,
    string Category,
    int? RelatedId);

public record DocumentDto(
    int DocumentId,
    string DocumentType,
    string FileName,
    string Status,
    DateTime UploadedAt,
    int? ShipmentId,
    string? ShipmentReference = null,
    int? FileSizeKb = null);

public record UpdateStatusRequest(string Status);

public record UserAdminDto(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? BusinessName,
    bool IsActive,
    bool IsVerified,
    DateTime CreatedAt,
    DateTime? LastLogin = null);

public record AdminStatsDto(
    int TotalUsers,
    int ActiveTraders,
    int TotalShipments,
    int PendingShipments,
    int TotalHsCodes,
    int ActiveSchemes);

public record ProfileDto(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? BusinessName,
    string? Ntn,
    string? Cnic,
    string? Phone,
    string? City,
    bool IsActive,
    bool IsVerified,
    bool NotifyShipments,
    bool NotifyForex,
    bool NotifyCompliance,
    bool TwoFactorEnabled,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record UpdateProfileRequest(
    string FullName,
    string? Phone,
    string? BusinessName,
    string? City,
    string? Ntn,
    string? Cnic,
    bool? NotifyShipments,
    bool? NotifyForex,
    bool? NotifyCompliance);

public record CreateShipmentRequest(
    string ShipmentType,
    int? PartyId,
    string ProductDesc,
    string HsCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal InvoiceValue,
    string Currency,
    string OriginCountry,
    string OriginPort,
    string DestinationCountry,
    string DestinationPort,
    DateOnly? ShipmentDate,
    DateOnly? ClearanceDate,
    string? Notes);

public record ShipmentDocumentDto(
    int DocumentId,
    string DocumentType,
    string DocumentName,
    string Status,
    DateTime UploadedAt);

public record CreateDocumentRequest(
    string DocumentType,
    string DocumentName,
    int? ShipmentId,
    string? Notes);

public record ComplianceStepDto(
    int StepId,
    int StepOrder,
    string StepName,
    string? Description,
    string? EstimatedTime,
    string? PortalUrl,
    bool IsCompleted,
    DateTime? CompletedAt,
    string? Notes);

public record UpdateComplianceRequest(bool IsCompleted, string? Notes);

public record SchemeDto(
    int SchemeId,
    string SchemeCode,
    string SchemeName,
    string? Agency,
    string? BenefitDetails,
    string? ApplicationDeadline,
    decimal MinExportPct,
    decimal MinTurnoverUsd,
    bool IsActive);

public record SchemeCheckRequest(
    string TraderType,
    int ExportPct,
    int TurnoverUsd,
    string Category,
    string FbrStatus);

public record SchemeCheckResultDto(
    string SchemeCode,
    string SchemeName,
    string? Agency,
    string? BenefitDetails,
    string? ApplicationDeadline,
    bool IsEligible);

public record SchemeCheckResponse(
    IReadOnlyList<SchemeCheckResultDto> EligibleSchemes,
    decimal EstimatedSavingsPkr);

public record AdminShipmentDto(
    int ShipmentId,
    string Reference,
    string TraderName,
    string? Company,
    string Type,
    string ProductDesc,
    string Value,
    string Status,
    DateTime CreatedAt);

public record UpdateUserStatusRequest(bool IsActive);

public record CreateHSCodeRequest(
    string Code,
    string Description,
    decimal CustomsDutyPct,
    decimal SalesTaxPct,
    decimal AitPct,
    string? UnitOfMeasure);

public record UpdateSchemeRequest(
    string SchemeCode,
    decimal MinExportPct,
    decimal MinTurnoverUsd,
    bool IsActive);

public record CreateSchemeRequest(
    string SchemeCode,
    string SchemeName,
    string? Agency,
    string? BenefitDetails,
    string? ApplicationDeadline,
    decimal MinExportPct,
    decimal MinTurnoverUsd,
    bool IsActive);

public record SystemSettingsDto(
    string TariffYear,
    bool MaintenanceMode,
    string? LastForexSync,
    string AppVersion);

public record UpdateSettingsRequest(string? TariffYear, bool? MaintenanceMode);

public record AdminReportDto(string Title, string Description, int Count, string ExportType);

public record AdminReportsDto(
    IReadOnlyList<AdminReportDto> Reports,
    IReadOnlyList<MonthlyUsageDto> MonthlyUsage);

public record MonthlyUsageDto(string Month, int ShipmentCount);
