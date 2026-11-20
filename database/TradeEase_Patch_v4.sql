-- TradeEase Patch v4: verified demo users, shipment origin country
USE TradeEaseDB;
GO

IF COL_LENGTH('dbo.Shipments', 'OriginCountry') IS NULL
    ALTER TABLE dbo.Shipments ADD OriginCountry NVARCHAR(64) NULL;
GO

-- Ahmed Raza: verified CNIC + NTN
UPDATE dbo.Users
SET CNIC = N'4210112345671',
    NTN = N'4780123-4',
    IsVerified = 1
WHERE Email = N'ahmed.raza@razaexports.pk';
GO

-- Second verified trader (skip if already exists)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'fatima.khan@textileexports.pk')
BEGIN
    INSERT INTO dbo.Users (RoleId, FullName, BusinessName, NTN, CNIC, Email, PasswordHash, Phone, City, IsVerified, IsActive, CreatedAt, LastLogin)
    VALUES (2, N'Fatima Khan', N'Textile Exports Ltd', N'1234567-8', N'3520276543210',
            N'fatima.khan@textileexports.pk', N'CHANGE_ME_HASH', N'+92-333-9988776', N'Faisalabad', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET CNIC = N'3520276543210', NTN = N'1234567-8', IsVerified = 1, BusinessName = N'Textile Exports Ltd'
    WHERE Email = N'fatima.khan@textileexports.pk';
END
GO

PRINT 'TradeEase patch v4 applied (verified users + OriginCountry).';
GO
