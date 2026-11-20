-- Extra HS codes for Pakistan import/export (FBR tariff FY 2025-26 style)
USE TradeEaseDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.HSCodes WHERE HSCode = N'84713000')
INSERT INTO dbo.HSCodes (HSCode, Description, UnitOfMeasure, CustomsDutyPct, SalesTaxPct, AITPct, TariffYear, IsActive) VALUES
(N'84713000', N'Portable digital computers (laptops)', N'NOS', 0, 17, 5.5, N'FY 2025-26', 1),
(N'85171200', N'Mobile phones / smartphones', N'NOS', 0, 17, 5.5, N'FY 2025-26', 1),
(N'87032300', N'Motor cars 1500-3000cc', N'NOS', 50, 17, 5.5, N'FY 2025-26', 1),
(N'30049099', N'Pharmaceutical products n.e.s.', N'KGS', 0, 17, 5.5, N'FY 2025-26', 1),
(N'72083900', N'Flat-rolled iron/steel products', N'KGS', 10, 17, 5.5, N'FY 2025-26', 1),
(N'04022100', N'Milk powder', N'KGS', 25, 17, 5.5, N'FY 2025-26', 1),
(N'27101900', N'Petroleum oils (medium)', N'LTR', 10, 17, 5.5, N'FY 2025-26', 1),
(N'61091000', N'Cotton T-shirts', N'NOS', 20, 17, 5.5, N'FY 2025-26', 1),
(N'63026000', N'Bed linen of cotton', N'NOS', 20, 17, 5.5, N'FY 2025-26', 1),
(N'71081200', N'Gold unwrought', N'GRM', 0, 17, 5.5, N'FY 2025-26', 1),
(N'09011100', N'Green coffee (not roasted)', N'KGS', 0, 17, 5.5, N'FY 2025-26', 1),
(N'17011400', N'Raw cane sugar', N'KGS', 15, 17, 5.5, N'FY 2025-26', 1),
(N'73089000', N'Steel structures and parts', N'KGS', 10, 17, 5.5, N'FY 2025-26', 1),
(N'85444900', N'Electric conductors insulated', N'KGS', 10, 17, 5.5, N'FY 2025-26', 1),
(N'39269099', N'Plastic articles n.e.s.', N'KGS', 20, 17, 5.5, N'FY 2025-26', 1);
GO

PRINT 'Extended HS tariff catalog seeded.';
GO
