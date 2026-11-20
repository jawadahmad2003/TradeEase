-- TradeEase Patch v2: CNIC + notification preferences (run once on existing DB)
USE TradeEaseDB;
GO

IF COL_LENGTH('dbo.Users', 'CNIC') IS NULL
    ALTER TABLE dbo.Users ADD CNIC NVARCHAR(13) NULL;
GO

IF COL_LENGTH('dbo.Users', 'NotifyShipments') IS NULL
    ALTER TABLE dbo.Users ADD NotifyShipments BIT NOT NULL CONSTRAINT DF_Users_NotifyShipments DEFAULT (1);
GO

IF COL_LENGTH('dbo.Users', 'NotifyForex') IS NULL
    ALTER TABLE dbo.Users ADD NotifyForex BIT NOT NULL CONSTRAINT DF_Users_NotifyForex DEFAULT (1);
GO

IF COL_LENGTH('dbo.Users', 'NotifyCompliance') IS NULL
    ALTER TABLE dbo.Users ADD NotifyCompliance BIT NOT NULL CONSTRAINT DF_Users_NotifyCompliance DEFAULT (1);
GO

PRINT 'TradeEase patch v2 applied.';
GO
