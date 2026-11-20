using Microsoft.EntityFrameworkCore;
using TradeEase.Models.Entities;

namespace TradeEase.Data;

public class TradeEaseDbContext : DbContext
{
    public TradeEaseDbContext(DbContextOptions<TradeEaseDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ComplianceStep> ComplianceSteps => Set<ComplianceStep>();
    public DbSet<UserComplianceProgress> UserComplianceProgress => Set<UserComplianceProgress>();
    public DbSet<TradeParty> TradeParties => Set<TradeParty>();
    public DbSet<HSCode> HSCodes => Set<HSCode>();
    public DbSet<ExportScheme> ExportSchemes => Set<ExportScheme>();
    public DbSet<EligibilityQuestion> EligibilityQuestions => Set<EligibilityQuestion>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistory => Set<ShipmentStatusHistory>();
    public DbSet<SchemeEligibilityResult> SchemeEligibilityResults => Set<SchemeEligibilityResult>();
    public DbSet<EligibilityAnswer> EligibilityAnswers => Set<EligibilityAnswer>();
    public DbSet<ForexRate> ForexRates => Set<ForexRate>();
    public DbSet<DutyCalculation> DutyCalculations => Set<DutyCalculation>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureRoles(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCompliance(modelBuilder);
        ConfigureTradeParties(modelBuilder);
        ConfigureHSCodes(modelBuilder);
        ConfigureSchemes(modelBuilder);
        ConfigureShipments(modelBuilder);
        ConfigureForex(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureSystemSettings(modelBuilder);
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.CNIC).HasMaxLength(13);
            e.Property(x => x.TwoFactorSecret).HasMaxLength(128);
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
        });
    }

    private static void ConfigureCompliance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComplianceStep>(e =>
        {
            e.ToTable("ComplianceSteps");
            e.HasKey(x => x.StepId);
            e.HasIndex(x => x.StepOrder).IsUnique();
        });

        modelBuilder.Entity<UserComplianceProgress>(e =>
        {
            e.ToTable("UserComplianceProgress");
            e.HasKey(x => x.ProgressId);
            e.HasIndex(x => new { x.UserId, x.StepId }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.ComplianceProgress).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Step).WithMany(x => x.UserProgress).HasForeignKey(x => x.StepId);
        });

        modelBuilder.Entity<EligibilityQuestion>(e =>
        {
            e.ToTable("EligibilityQuestions");
            e.HasKey(x => x.QuestionId);
        });

        modelBuilder.Entity<EligibilityAnswer>(e =>
        {
            e.ToTable("EligibilityAnswers");
            e.HasKey(x => x.AnswerId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Shipment).WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Question).WithMany(x => x.Answers).HasForeignKey(x => x.QuestionId);
        });

        modelBuilder.Entity<SchemeEligibilityResult>(e =>
        {
            e.ToTable("SchemeEligibilityResults");
            e.HasKey(x => x.ResultId);
            e.HasOne(x => x.Shipment).WithMany(x => x.SchemeResults).HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Scheme).WithMany(x => x.Results).HasForeignKey(x => x.SchemeId);
        });
    }

    private static void ConfigureTradeParties(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeParty>(e =>
        {
            e.ToTable("TradeParties");
            e.HasKey(x => x.PartyId);
            e.HasOne(x => x.User).WithMany(x => x.TradeParties).HasForeignKey(x => x.UserId);
        });
    }

    private static void ConfigureHSCodes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HSCode>(e =>
        {
            e.ToTable("HSCodes");
            e.HasKey(x => x.HSCodeId);
            e.Property(x => x.Code).HasColumnName("HSCode").HasMaxLength(12).IsRequired();
            e.HasIndex(x => new { x.Code, x.TariffYear }).IsUnique();
        });

        modelBuilder.Entity<DutyCalculation>(e =>
        {
            e.ToTable("DutyCalculations");
            e.HasKey(x => x.CalculationId);
            e.HasOne(x => x.Shipment).WithMany(x => x.DutyCalculations).HasForeignKey(x => x.ShipmentId);
            e.HasOne(x => x.HSCode).WithMany(x => x.DutyCalculations).HasForeignKey(x => x.HSCodeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ForexRate).WithMany(x => x.DutyCalculations).HasForeignKey(x => x.RateId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureSchemes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExportScheme>(e =>
        {
            e.ToTable("ExportSchemes");
            e.HasKey(x => x.SchemeId);
            e.HasIndex(x => x.SchemeCode).IsUnique();
        });
    }

    private static void ConfigureShipments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(e =>
        {
            e.ToTable("Shipments", tb => tb.HasTrigger("trg_ShipmentStatusAudit"));
            e.HasKey(x => x.ShipmentId);
            e.HasIndex(x => x.ShipmentReference).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.Shipments).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Party).WithMany(x => x.Shipments).HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.HSCode).WithMany(x => x.Shipments).HasForeignKey(x => x.HSCodeId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ShipmentStatusHistory>(e =>
        {
            e.ToTable("ShipmentStatusHistory");
            e.HasKey(x => x.HistoryId);
            e.HasOne(x => x.Shipment).WithMany(x => x.StatusHistory).HasForeignKey(x => x.ShipmentId);
            e.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureForex(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForexRate>(e =>
        {
            e.ToTable("ForexRates");
            e.HasKey(x => x.RateId);
            e.HasIndex(x => new { x.CurrencyCode, x.RateDate }).IsUnique();
        });
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("Documents");
            e.HasKey(x => x.DocumentId);
            e.HasOne(x => x.Shipment).WithMany(x => x.Documents).HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.User).WithMany(x => x.Documents).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.VerifiedByUser).WithMany().HasForeignKey(x => x.VerifiedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.ToTable("SystemSettings");
            e.HasKey(x => x.SettingKey);
            e.Property(x => x.SettingKey).HasMaxLength(100);
            e.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
