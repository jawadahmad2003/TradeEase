/* =============================================================================
   TradeEase ERP — SQL Server Database Schema
   FIRST RUN ONLY — or run TradeEase_Reset.sql to wipe and recreate.
   IMPORTANT: Script starts on master so re-runs can drop the database safely.
   ============================================================================= */

USE master;
GO

-- ─── STEP 1: Create database ─────────────────────────────────────────────────
IF DB_ID(N'TradeEaseDB') IS NOT NULL
BEGIN
    ALTER DATABASE TradeEaseDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TradeEaseDB;
END;
GO

CREATE DATABASE TradeEaseDB;
GO

USE TradeEaseDB;
GO

-- ─── STEP 2: Create tables (dependency order) ──────────────────────────────

-- M-01: Roles & Users
CREATE TABLE dbo.Roles (
    RoleId          INT             NOT NULL IDENTITY(1,1),
    RoleName        NVARCHAR(50)    NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

CREATE TABLE dbo.Users (
    UserId          INT             NOT NULL IDENTITY(1,1),
    RoleId          INT             NOT NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    BusinessName    NVARCHAR(200)   NULL,
    NTN             NVARCHAR(20)    NULL,
    CNIC            NVARCHAR(20)    NULL,
    Email           NVARCHAR(256)   NOT NULL,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    Phone           NVARCHAR(30)    NULL,
    City            NVARCHAR(100)   NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    IsVerified      BIT             NOT NULL CONSTRAINT DF_Users_IsVerified DEFAULT (0),
    NotifyShipments BIT             NOT NULL CONSTRAINT DF_Users_NotifyShipments DEFAULT (1),
    NotifyForex     BIT             NOT NULL CONSTRAINT DF_Users_NotifyForex DEFAULT (1),
    NotifyCompliance BIT            NOT NULL CONSTRAINT DF_Users_NotifyCompliance DEFAULT (1),
    TwoFactorEnabled BIT            NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0),
    TwoFactorSecret NVARCHAR(100)   NULL,
    LastLogin       DATETIME2(0)    NULL,
    CreatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
);

-- M-07: Compliance wizard steps
CREATE TABLE dbo.ComplianceSteps (
    StepId          INT             NOT NULL IDENTITY(1,1),
    StepOrder       INT             NOT NULL,
    StepName        NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(500)   NULL,
    EstimatedTime   NVARCHAR(50)    NULL,
    PortalUrl       NVARCHAR(500)   NULL,
    CONSTRAINT PK_ComplianceSteps PRIMARY KEY (StepId),
    CONSTRAINT UQ_ComplianceSteps_StepOrder UNIQUE (StepOrder)
);

CREATE TABLE dbo.UserComplianceProgress (
    ProgressId      INT             NOT NULL IDENTITY(1,1),
    UserId          INT             NOT NULL,
    StepId          INT             NOT NULL,
    IsCompleted     BIT             NOT NULL CONSTRAINT DF_UserComplianceProgress_IsCompleted DEFAULT (0),
    CompletedAt     DATETIME2(0)    NULL,
    Notes           NVARCHAR(500)   NULL,
    CONSTRAINT PK_UserComplianceProgress PRIMARY KEY (ProgressId),
    CONSTRAINT UQ_UserComplianceProgress_UserStep UNIQUE (UserId, StepId),
    CONSTRAINT FK_UserComplianceProgress_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_UserComplianceProgress_Steps FOREIGN KEY (StepId) REFERENCES dbo.ComplianceSteps (StepId)
);

-- M-02: Trade parties (owned by trader)
CREATE TABLE dbo.TradeParties (
    PartyId         INT             NOT NULL IDENTITY(1,1),
    UserId          INT             NOT NULL,
    PartyType       NVARCHAR(30)    NOT NULL,   -- Buyer, Supplier, Agent, Forwarder
    Name            NVARCHAR(200)   NOT NULL,
    City            NVARCHAR(100)   NULL,
    Country         NVARCHAR(100)   NULL,
    Address         NVARCHAR(300)   NULL,
    Email           NVARCHAR(256)   NULL,
    Phone           NVARCHAR(30)    NULL,
    TaxId           NVARCHAR(30)    NULL,
    RegistrationNo  NVARCHAR(50)    NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_TradeParties_IsActive DEFAULT (1),
    CreatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_TradeParties_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_TradeParties PRIMARY KEY (PartyId),
    CONSTRAINT FK_TradeParties_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_TradeParties_PartyType CHECK (PartyType IN (N'Buyer', N'Supplier', N'Agent', N'Forwarder'))
);

-- M-05: FBR Mock Registry (for dummy validation)
CREATE TABLE dbo.FbrMockRegistry (
    Id              INT             NOT NULL IDENTITY(1,1),
    CNIC            NVARCHAR(20)    NOT NULL,
    NTN             NVARCHAR(20)    NOT NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    BusinessName    NVARCHAR(200)   NOT NULL,
    AtlStatus       NVARCHAR(20)    NOT NULL CONSTRAINT DF_FbrMockRegistry_AtlStatus DEFAULT ('Active'),
    CONSTRAINT PK_FbrMockRegistry PRIMARY KEY (Id)
);

-- M-06: FBR HS tariff
CREATE TABLE dbo.HSCodes (
    HSCodeId        INT             NOT NULL IDENTITY(1,1),
    HSCode          NVARCHAR(12)    NOT NULL,
    Description     NVARCHAR(500)   NOT NULL,
    Chapter         NVARCHAR(10)    NULL,
    Heading         NVARCHAR(10)    NULL,
    Subheading      NVARCHAR(10)    NULL,
    Section         NVARCHAR(10)    NULL,
    UnitOfMeasure   NVARCHAR(20)    NULL,
    CustomsDutyPct  DECIMAL(8,4)    NOT NULL CONSTRAINT DF_HSCodes_Customs DEFAULT (0),
    SalesTaxPct     DECIMAL(8,4)    NOT NULL CONSTRAINT DF_HSCodes_SalesTax DEFAULT (0),
    AITPct          DECIMAL(8,4)    NOT NULL CONSTRAINT DF_HSCodes_AIT DEFAULT (0),
    TariffYear      NVARCHAR(20)    NOT NULL CONSTRAINT DF_HSCodes_TariffYear DEFAULT (N'FY 2025-26'),
    IsActive        BIT             NOT NULL CONSTRAINT DF_HSCodes_IsActive DEFAULT (1),
    CONSTRAINT PK_HSCodes PRIMARY KEY (HSCodeId),
    CONSTRAINT UQ_HSCodes_CodeYear UNIQUE (HSCode, TariffYear)
);

-- M-08: Export schemes + admin rules
CREATE TABLE dbo.ExportSchemes (
    SchemeId            INT             NOT NULL IDENTITY(1,1),
    SchemeCode          NVARCHAR(20)    NOT NULL,
    SchemeName          NVARCHAR(150)   NOT NULL,
    Agency              NVARCHAR(150)   NULL,
    BenefitDetails      NVARCHAR(1000)  NULL,
    ApplicationDeadline NVARCHAR(100)   NULL,
    MinExportPct        DECIMAL(8,4)    NOT NULL CONSTRAINT DF_ExportSchemes_MinExport DEFAULT (0),
    MinTurnoverUsd      DECIMAL(18,2)   NOT NULL CONSTRAINT DF_ExportSchemes_MinTurnover DEFAULT (0),
    IsActive            BIT             NOT NULL CONSTRAINT DF_ExportSchemes_IsActive DEFAULT (1),
    CONSTRAINT PK_ExportSchemes PRIMARY KEY (SchemeId),
    CONSTRAINT UQ_ExportSchemes_Code UNIQUE (SchemeCode)
);

-- M-08: Eligibility questions (supports Scheme Checker)
CREATE TABLE dbo.EligibilityQuestions (
    QuestionId          INT             NOT NULL IDENTITY(1,1),
    QuestionOrder       INT             NOT NULL,
    QuestionText        NVARCHAR(300)   NOT NULL,
    QuestionCategory    NVARCHAR(50)    NULL,
    QuestionType        NVARCHAR(30)    NOT NULL,  -- Select, Number, Boolean
    CONSTRAINT PK_EligibilityQuestions PRIMARY KEY (QuestionId)
);

-- M-03: Shipments
CREATE TABLE dbo.Shipments (
    ShipmentId          INT             NOT NULL IDENTITY(1,1),
    ShipmentReference   NVARCHAR(30)    NOT NULL,
    UserId              INT             NOT NULL,
    PartyId             INT             NULL,
    HSCodeId            INT             NULL,
    ShipmentType        NVARCHAR(10)    NOT NULL,  -- Import, Export
    ProductDesc         NVARCHAR(300)   NOT NULL,
    InvoiceValue        DECIMAL(18,2)   NOT NULL,
    Currency            NVARCHAR(3)     NOT NULL CONSTRAINT DF_Shipments_Currency DEFAULT (N'USD'),
    OriginPort          NVARCHAR(150)   NULL,
    OriginCountry       NVARCHAR(100)   NULL,
    DestinationPort     NVARCHAR(150)   NULL,
    DestinationCountry  NVARCHAR(100)   NULL,
    Quantity            DECIMAL(18,4)   NULL,
    UnitOfMeasure       NVARCHAR(20)    NULL,
    Weight              DECIMAL(18,4)   NULL,
    WeightUnit          NVARCHAR(10)    NULL,
    TotalValuePKR       DECIMAL(18,2)   NULL,
    ShipmentStatus      NVARCHAR(30)    NOT NULL CONSTRAINT DF_Shipments_Status DEFAULT (N'Booked'),
    ShipmentDate        DATE            NULL,
    ClearanceDate       DATE            NULL,
    Notes               NVARCHAR(1000)  NULL,
    CreatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_Shipments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Shipments PRIMARY KEY (ShipmentId),
    CONSTRAINT UQ_Shipments_Reference UNIQUE (ShipmentReference),
    CONSTRAINT FK_Shipments_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Shipments_Parties FOREIGN KEY (PartyId) REFERENCES dbo.TradeParties (PartyId),
    CONSTRAINT FK_Shipments_HSCodes FOREIGN KEY (HSCodeId) REFERENCES dbo.HSCodes (HSCodeId),
    CONSTRAINT CK_Shipments_Type CHECK (ShipmentType IN (N'Import', N'Export')),
    CONSTRAINT CK_Shipments_Status CHECK (ShipmentStatus IN (
        N'Booked', N'Loaded', N'In Transit', N'At Port', N'Under Customs', N'Cleared'
    ))
);

CREATE TABLE dbo.ShipmentStatusHistory (
    HistoryId       INT             NOT NULL IDENTITY(1,1),
    ShipmentId      INT             NOT NULL,
    Status          NVARCHAR(30)    NOT NULL,
    Remarks         NVARCHAR(500)   NULL,
    ChangedBy       INT             NULL,
    ChangedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_ShipmentStatusHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ShipmentStatusHistory PRIMARY KEY (HistoryId),
    CONSTRAINT FK_ShipmentStatusHistory_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (ShipmentId),
    CONSTRAINT FK_ShipmentStatusHistory_Users FOREIGN KEY (ChangedBy) REFERENCES dbo.Users (UserId)
);

-- M-08: Scheme eligibility results
CREATE TABLE dbo.SchemeEligibilityResults (
    ResultId            INT             NOT NULL IDENTITY(1,1),
    ShipmentId          INT             NULL,
    UserId              INT             NOT NULL,
    SchemeId            INT             NOT NULL,
    IsEligible          BIT             NOT NULL,
    EligibilityScore    DECIMAL(8,2)    NULL,
    CheckedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_SchemeEligibilityResults_CheckedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_SchemeEligibilityResults PRIMARY KEY (ResultId),
    CONSTRAINT FK_SchemeEligibilityResults_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (ShipmentId),
    CONSTRAINT FK_SchemeEligibilityResults_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_SchemeEligibilityResults_Schemes FOREIGN KEY (SchemeId) REFERENCES dbo.ExportSchemes (SchemeId)
);

CREATE TABLE dbo.EligibilityAnswers (
    AnswerId            INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    ShipmentId          INT             NULL,
    QuestionId          INT             NOT NULL,
    AnswerText          NVARCHAR(300)   NULL,
    AnswerValue         NVARCHAR(100)   NULL,
    IsTrue              BIT             NULL,
    ScoreContribution   DECIMAL(8,2)    NULL,
    AnsweredAt          DATETIME2(0)    NOT NULL CONSTRAINT DF_EligibilityAnswers_AnsweredAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_EligibilityAnswers PRIMARY KEY (AnswerId),
    CONSTRAINT FK_EligibilityAnswers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_EligibilityAnswers_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (ShipmentId),
    CONSTRAINT FK_EligibilityAnswers_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.EligibilityQuestions (QuestionId)
);

-- M-06: Forex rates
CREATE TABLE dbo.ForexRates (
    RateId          INT             NOT NULL IDENTITY(1,1),
    CurrencyCode    NVARCHAR(3)     NOT NULL,
    ExchangeRate    DECIMAL(18,6)   NOT NULL,
    RateDate        DATE            NOT NULL,
    RateSource      NVARCHAR(100)   NOT NULL CONSTRAINT DF_ForexRates_Source DEFAULT (N'frankfurter.app'),
    CreatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_ForexRates_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ForexRates PRIMARY KEY (RateId),
    CONSTRAINT UQ_ForexRates_CurrencyDate UNIQUE (CurrencyCode, RateDate)
);

-- M-05: Duty calculations
CREATE TABLE dbo.DutyCalculations (
    CalculationId       INT             NOT NULL IDENTITY(1,1),
    ShipmentId          INT             NOT NULL,
    HSCodeId            INT             NULL,
    RateId              INT             NULL,
    DutyType            NVARCHAR(30)    NOT NULL,  -- Customs, SalesTax, AIT, Total
    DutyRate            DECIMAL(8,4)    NULL,
    CalculatedAmount    DECIMAL(18,2)   NOT NULL,
    Currency            NVARCHAR(3)     NOT NULL,
    ExchangeRate        DECIMAL(18,6)   NULL,
    TotalTax            DECIMAL(18,2)   NULL,
    TotalDuty           DECIMAL(18,2)   NULL,
    NetPayable          DECIMAL(18,2)   NULL,
    CalculatedAt        DATETIME2(0)    NOT NULL CONSTRAINT DF_DutyCalculations_CalculatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_DutyCalculations PRIMARY KEY (CalculationId),
    CONSTRAINT FK_DutyCalculations_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (ShipmentId),
    CONSTRAINT FK_DutyCalculations_HSCodes FOREIGN KEY (HSCodeId) REFERENCES dbo.HSCodes (HSCodeId),
    CONSTRAINT FK_DutyCalculations_ForexRates FOREIGN KEY (RateId) REFERENCES dbo.ForexRates (RateId)
);

-- M-04: Documents
CREATE TABLE dbo.Documents (
    DocumentId      INT             NOT NULL IDENTITY(1,1),
    ShipmentId      INT             NULL,
    UserId          INT             NOT NULL,
    DocumentType    NVARCHAR(50)    NOT NULL,
    DocumentName    NVARCHAR(300)   NOT NULL,
    FilePath        NVARCHAR(500)   NULL,
    FileSizeKb      INT             NULL,
    Status          NVARCHAR(30)    NOT NULL CONSTRAINT DF_Documents_Status DEFAULT (N'Generated'),
    UploadedAt      DATETIME2(0)    NOT NULL CONSTRAINT DF_Documents_UploadedAt DEFAULT (SYSUTCDATETIME()),
    VerifiedBy      INT             NULL,
    VerifiedAt      DATETIME2(0)    NULL,
    Notes           NVARCHAR(500)   NULL,
    CONSTRAINT PK_Documents PRIMARY KEY (DocumentId),
    CONSTRAINT FK_Documents_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (ShipmentId),
    CONSTRAINT FK_Documents_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Documents_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Documents_Status CHECK (Status IN (N'Generated', N'Uploaded', N'Verified', N'Archived'))
);

-- Admin system settings
CREATE TABLE dbo.SystemSettings (
    SettingKey      NVARCHAR(100)   NOT NULL,
    SettingValue    NVARCHAR(500)   NOT NULL,
    UpdatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedBy       INT             NULL,
    CONSTRAINT PK_SystemSettings PRIMARY KEY (SettingKey),
    CONSTRAINT FK_SystemSettings_Users FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users (UserId)
);

-- ─── STEP 3: Indexes for performance ───────────────────────────────────────
CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
CREATE INDEX IX_TradeParties_UserId ON dbo.TradeParties (UserId);
CREATE INDEX IX_Shipments_UserId ON dbo.Shipments (UserId);
CREATE INDEX IX_Shipments_Status ON dbo.Shipments (ShipmentStatus);
CREATE INDEX IX_Shipments_CreatedAt ON dbo.Shipments (CreatedAt DESC);
CREATE INDEX IX_Documents_ShipmentId ON dbo.Documents (ShipmentId);
CREATE INDEX IX_DutyCalculations_ShipmentId ON dbo.DutyCalculations (ShipmentId);
CREATE INDEX IX_ShipmentStatusHistory_ShipmentId ON dbo.ShipmentStatusHistory (ShipmentId);
CREATE INDEX IX_ForexRates_RateDate ON dbo.ForexRates (RateDate DESC);

GO

-- ─── STEP 4: Seed reference data ───────────────────────────────────────────

INSERT INTO dbo.Roles (RoleName) VALUES (N'Admin'), (N'Trader');

INSERT INTO dbo.ComplianceSteps (StepOrder, StepName, Description, EstimatedTime, PortalUrl) VALUES
(1, N'NTN Registration (FBR IRIS)', N'Register National Tax Number with FBR', N'1-2 business days', N'https://iris.fbr.gov.pk'),
(2, N'SECP Company Registration', N'Register private limited company with SECP', N'3-5 business days', N'https://eservices.secp.gov.pk'),
(3, N'Chamber of Commerce Membership', N'Obtain export membership certificate', N'2-3 business days', N'https://www.kcci.org.pk'),
(4, N'WeBOC Registration', N'Register on Pakistan Customs web portal', N'5-7 business days', N'https://weboc.gov.pk'),
(5, N'Trade Bank Account', N'Open export-oriented bank account', N'1-3 business days', NULL);

INSERT INTO dbo.ExportSchemes (SchemeCode, SchemeName, Agency, BenefitDetails, ApplicationDeadline, MinExportPct, MinTurnoverUsd, IsActive) VALUES
(N'EFS',     N'Export Finance Scheme',              N'State Bank of Pakistan', N'Concessional export financing at reduced markup rates.', N'Rolling applications', 30, 50000, 1),
(N'DDT',     N'Duty Drawback Scheme',               N'FBR / Pakistan Customs', N'Refund of customs duties on imported inputs used in exports.', N'Within 120 days of export', 20, 0, 1),
(N'SBP-EFS', N'SBP Export Finance Scheme',          N'State Bank of Pakistan', N'Long-term export financing for eligible exporters.', N'Quarterly windows', 0, 100000, 1),
(N'EMDF',    N'Export Marketing Development Fund',  N'TDAP',                   N'Reimbursement for export marketing and trade fair expenses.', N'31-Dec-2026', 10, 25000, 1),
(N'LTFF',    N'Long Term Financing Facility',       N'State Bank of Pakistan', N'Financing for plant and machinery imports for export sectors.', N'Ongoing', 0, 500000, 0);

INSERT INTO dbo.EligibilityQuestions (QuestionOrder, QuestionText, QuestionCategory, QuestionType) VALUES
(1, N'Trader Type', N'Profile', N'Select'),
(2, N'Export Percentage of Turnover', N'Export', N'Select'),
(3, N'Annual Turnover (USD)', N'Finance', N'Select'),
(4, N'Primary Export Category', N'Product', N'Select'),
(5, N'FBR Filing Status', N'Compliance', N'Select');

INSERT INTO dbo.HSCodes (HSCode, Description, Chapter, Heading, CustomsDutyPct, SalesTaxPct, AITPct, UnitOfMeasure) VALUES
(N'52082100', N'Cotton fabric, dyed plain weave', N'52', N'5208', 5.00, 17.00, 5.50, N'MTR'),
(N'10063000', N'Semi-milled rice', N'10', N'1006', 0.00, 0.00, 1.00, N'KG'),
(N'90189000', N'Medical instruments', N'90', N'9018', 0.00, 0.00, 1.00, N'NOS'),
(N'08052000', N'Oranges, fresh', N'08', N'0805', 20.00, 18.00, 5.50, N'KG');

INSERT INTO dbo.ForexRates (CurrencyCode, ExchangeRate, RateDate, RateSource) VALUES
(N'USD', 278.400000, CAST(GETDATE() AS DATE), N'frankfurter.app'),
(N'EUR', 302.150000, CAST(GETDATE() AS DATE), N'frankfurter.app'),
(N'GBP', 352.800000, CAST(GETDATE() AS DATE), N'frankfurter.app');

INSERT INTO dbo.SystemSettings (SettingKey, SettingValue) VALUES
(N'TariffYear', N'FY 2025-26'),
(N'MaintenanceMode', N'false'),
(N'ForexApiUrl', N'https://api.frankfurter.app/latest?from=USD&to=PKR'),
(N'AppVersion', N'1.0.0');

-- Demo users (PasswordHash = placeholder — replace with ASP.NET Identity hash in production)
INSERT INTO dbo.Users (RoleId, FullName, BusinessName, NTN, Email, PasswordHash, Phone, City, IsVerified, LastLogin)
VALUES
(1, N'Hina Malik', N'TradeEase Platform', NULL, N'admin@tradeease.pk', N'CHANGE_ME_HASH', N'+92-300-1122334', N'Lahore', 1, SYSUTCDATETIME()),
(2, N'Ahmed Raza', N'Raza Exports Pvt Ltd', N'4780123-4', N'ahmed.raza@razaexports.pk', N'CHANGE_ME_HASH', N'+92-321-5678901', N'Karachi', 1, SYSUTCDATETIME());

GO

PRINT 'TradeEaseDB created successfully with 15 tables and seed data.';
GO
