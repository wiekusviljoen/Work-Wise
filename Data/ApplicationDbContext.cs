using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<EmploymentContractRecord> EmploymentContracts => Set<EmploymentContractRecord>();
    public DbSet<LeaveRequestRecord> LeaveRequests => Set<LeaveRequestRecord>();
    public DbSet<LeaveTransaction> LeaveTransactions => Set<LeaveTransaction>();
    public DbSet<BenefitReminder> BenefitReminders => Set<BenefitReminder>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>().HasIndex(x => x.Name);
        builder.Entity<Organization>().Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Entity<Organization>().Property(x => x.CountryCode).HasMaxLength(2).IsRequired();

        builder.Entity<OrganizationMembership>()
            .HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        builder.Entity<OrganizationMembership>().Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Entity<OrganizationMembership>()
            .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EmployeeProfile>()
            .HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        builder.Entity<EmployeeProfile>().Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Entity<EmployeeProfile>().Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Entity<EmployeeProfile>().Property(x => x.GrossMonthlySalary).HasPrecision(18, 2);
        builder.Entity<EmployeeProfile>()
            .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EmploymentContractRecord>().HasIndex(x => x.EmployeeProfileId);
        builder.Entity<EmploymentContractRecord>().Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Entity<EmploymentContractRecord>().Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Entity<EmploymentContractRecord>().Property(x => x.ExtractedTermsJson).HasMaxLength(30000);

        builder.Entity<LeaveRequestRecord>().HasIndex(x => new { x.EmployeeProfileId, x.StartDate, x.EndDate });
        builder.Entity<LeaveRequestRecord>().Property(x => x.LeaveType).HasMaxLength(40).IsRequired();
        builder.Entity<LeaveRequestRecord>().Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Entity<LeaveRequestRecord>().Property(x => x.RequestedWorkingDays).HasPrecision(8, 2);

        builder.Entity<LeaveTransaction>().HasIndex(x => new { x.EmployeeProfileId, x.LeaveType, x.EffectiveDate });
        builder.Entity<LeaveTransaction>().Property(x => x.QuantityDays).HasPrecision(8, 2);
        builder.Entity<LeaveTransaction>().Property(x => x.TransactionType).HasMaxLength(32).IsRequired();
        builder.Entity<LeaveTransaction>().Property(x => x.Source).HasMaxLength(80).IsRequired();

        builder.Entity<BenefitReminder>().HasIndex(x => new { x.EmployeeProfileId, x.ExpectedPaymentDate });
        builder.Entity<BenefitReminder>().Property(x => x.EstimatedAmount).HasPrecision(18, 2);
        builder.Entity<BenefitReminder>().Property(x => x.Status).HasMaxLength(32).IsRequired();

        builder.Entity<AuditEvent>().HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
        builder.Entity<AuditEvent>().Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Entity<AuditEvent>().Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Entity<AuditEvent>().Property(x => x.EntityId).HasMaxLength(100).IsRequired();
    }
}