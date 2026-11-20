/* =============================================================================
   TradeEase ERP — Reporting API Schema Objects
   Contains: Views, Stored Procedures, and Triggers for advanced reporting.
   ============================================================================= */

USE TradeEaseDB;
GO

-- ─── STEP 1: Views ───────────────────────────────────────────────────────────

-- vw_ShipmentComprehensive: Flattens shipment data for fast reporting
CREATE OR ALTER VIEW dbo.vw_ShipmentComprehensive AS
SELECT 
    s.ShipmentId,
    s.ShipmentReference,
    s.ShipmentType,
    s.ShipmentStatus,
    s.InvoiceValue,
    s.Currency,
    s.OriginCountry,
    s.DestinationCountry,
    s.OriginPort,
    s.DestinationPort,
    s.ShipmentDate,
    s.ClearanceDate,
    s.CreatedAt,
    
    u.FullName AS UserFullName,
    u.Email AS UserEmail,
    u.BusinessName AS UserBusinessName,
    
    p.Name AS PartyName,
    p.PartyType AS PartyType,
    p.Country AS PartyCountry,
    
    h.HSCode,
    h.Description AS HSDescription,
    h.Chapter AS HSChapter,
    h.CustomsDutyPct,
    h.SalesTaxPct
FROM dbo.Shipments s
INNER JOIN dbo.Users u ON s.UserId = u.UserId
LEFT JOIN dbo.TradeParties p ON s.PartyId = p.PartyId
LEFT JOIN dbo.HSCodes h ON s.HSCodeId = h.HSCodeId;
GO

-- ─── STEP 2: Stored Procedures ───────────────────────────────────────────────

-- sp_GenerateShipmentReport: Dynamic filtering for shipments
CREATE OR ALTER PROCEDURE dbo.sp_GenerateShipmentReport
    @StartDate DATETIME2(0) = NULL,
    @EndDate DATETIME2(0) = NULL,
    @ShipmentType NVARCHAR(30) = NULL,
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_ShipmentComprehensive
    WHERE (@StartDate IS NULL OR CreatedAt >= @StartDate)
      AND (@EndDate IS NULL OR CreatedAt <= @EndDate)
      AND (@ShipmentType IS NULL OR ShipmentType = @ShipmentType)
      AND (@Status IS NULL OR ShipmentStatus = @Status)
    ORDER BY CreatedAt DESC;
END;
GO

-- sp_GetTopImportExportCountries: Aggregation procedure
CREATE OR ALTER PROCEDURE dbo.sp_GetTopImportExportCountries
    @TopN INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
        ShipmentType,
        DestinationCountry AS Country,
        COUNT(ShipmentId) AS TotalShipments,
        SUM(InvoiceValue) AS TotalValue
    FROM dbo.Shipments
    WHERE ShipmentType = 'Export' AND DestinationCountry IS NOT NULL
    GROUP BY ShipmentType, DestinationCountry
    ORDER BY TotalValue DESC;

    SELECT TOP (@TopN)
        ShipmentType,
        OriginCountry AS Country,
        COUNT(ShipmentId) AS TotalShipments,
        SUM(InvoiceValue) AS TotalValue
    FROM dbo.Shipments
    WHERE ShipmentType = 'Import' AND OriginCountry IS NOT NULL
    GROUP BY ShipmentType, OriginCountry
    ORDER BY TotalValue DESC;
END;
GO

-- ─── STEP 3: Triggers ────────────────────────────────────────────────────────

-- trg_ShipmentStatusAudit: Automatic audit trailing for shipment status
CREATE OR ALTER TRIGGER dbo.trg_ShipmentStatusAudit
ON dbo.Shipments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only insert history if Status actually changed
    IF UPDATE(ShipmentStatus)
    BEGIN
        INSERT INTO dbo.ShipmentStatusHistory (ShipmentId, Status, Remarks, ChangedBy, ChangedAt)
        SELECT 
            i.ShipmentId,
            i.ShipmentStatus,
            'Status changed from ' + ISNULL(d.ShipmentStatus, 'None') + ' to ' + ISNULL(i.ShipmentStatus, 'None') + ' via trigger',
            NULL, -- ChangedBy null for automated trigger
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.ShipmentId = d.ShipmentId
        WHERE i.ShipmentStatus <> d.ShipmentStatus OR (i.ShipmentStatus IS NOT NULL AND d.ShipmentStatus IS NULL) OR (i.ShipmentStatus IS NULL AND d.ShipmentStatus IS NOT NULL);
    END
END;
GO
