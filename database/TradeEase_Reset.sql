/* Reset TradeEaseDB — run ONLY when you want to delete everything and start fresh */
USE master;
GO

IF DB_ID(N'TradeEaseDB') IS NOT NULL
BEGIN
    ALTER DATABASE TradeEaseDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TradeEaseDB;
    PRINT 'TradeEaseDB dropped. Now run TradeEase_Schema.sql once.';
END
ELSE
    PRINT 'TradeEaseDB does not exist — run TradeEase_Schema.sql.';
GO
