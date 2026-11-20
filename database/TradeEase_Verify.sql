/* Verify TradeEaseDB — run anytime (safe, read-only) */
USE TradeEaseDB;
GO

SELECT 'Tables' AS CheckType, COUNT(*) AS ItemCount
FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'
UNION ALL
SELECT 'Roles', COUNT(*) FROM dbo.Roles
UNION ALL
SELECT 'Users', COUNT(*) FROM dbo.Users
UNION ALL
SELECT 'HSCodes', COUNT(*) FROM dbo.HSCodes
UNION ALL
SELECT 'ExportSchemes', COUNT(*) FROM dbo.ExportSchemes
UNION ALL
SELECT 'Shipments', COUNT(*) FROM dbo.Shipments
UNION ALL
SELECT 'TradeParties', COUNT(*) FROM dbo.TradeParties;

SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;

SELECT r.RoleName, u.FullName, u.Email, u.IsVerified
FROM dbo.Users u
JOIN dbo.Roles r ON r.RoleId = u.RoleId;
