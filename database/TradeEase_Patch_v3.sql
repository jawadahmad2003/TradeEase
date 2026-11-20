-- TradeEase Patch v3: 2FA columns
USE TradeEaseDB;
GO

IF COL_LENGTH('dbo.Users', 'TwoFactorEnabled') IS NULL
    ALTER TABLE dbo.Users ADD TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0);
GO

IF COL_LENGTH('dbo.Users', 'TwoFactorSecret') IS NULL
    ALTER TABLE dbo.Users ADD TwoFactorSecret NVARCHAR(128) NULL;
GO

PRINT 'TradeEase patch v3 applied.';
GO
