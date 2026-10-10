using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<EmploymentRecord> EmploymentRecords => Set<EmploymentRecord>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();
    public DbSet<ContractClause> ContractClauses => Set<ContractClause>();
    public DbSet<LeaveRequestRecord> LeaveRequests => Set<LeaveRequestRecord>();
    public DbSet<LeaveLedgerRecord> LeaveLedger => Set<LeaveLedgerRecord>();
    public DbSet<OrganizationLeavePolicy> OrganizationLeavePolicies => Set<OrganizationLeavePolicy>();
    public DbSet<BenefitReminder> BenefitReminders => Set<BenefitReminder>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>().HasIndex(x => new { x.CountryCode, x.Name });
        builder.Entity<OrganizationMembership>().HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        builder.Entity<OrganizationMembership>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmploymentRecord>().HasIndex(x => new { x.OrganizationId, x.EmployeeEmail });
        builder.Entity<EmploymentRecord>().HasOne(x => x.EmployeeUser).WithMany().HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<EmploymentRecord>().HasOne(x => x.Organization).WithMany(x => x.Employments).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EmploymentContract>().HasOne(x => x.EmploymentRecord).WithMany(x => x.Contracts).HasForeignKey(x => x.EmploymentRecordId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ContractClause>().HasOne(x => x.EmploymentContract).WithMany(x => x.Clauses).HasForeignKey(x => x.EmploymentContractId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LeaveRequestRecord>().HasOne(x => x.EmploymentRecord).WithMany(x => x.LeaveRequests).HasForeignKey(x => x.EmploymentRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LeaveLedgerRecord>().HasOne(x => x.EmploymentRecord).WithMany(x => x.LeaveLedger).HasForeignKey(x => x.EmploymentRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BenefitReminder>().HasOne(x => x.EmploymentRecord).WithMany(x => x.BenefitReminders).HasForeignKey(x => x.EmploymentRecordId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AuditEvent>().HasIndex(x => new { x.OrganizationId, x.OccurredUtc });
        builder.Entity<AuditEvent>().HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LeaveLedgerRecord>().HasIndex(x => new { x.EmploymentRecordId, x.LeaveType, x.EffectiveDate });
        builder.Entity<LeaveLedgerRecord>().HasIndex(x => new { x.EmploymentRecordId, x.EntryType, x.Reference }).IsUnique();
        builder.Entity<OrganizationLeavePolicy>().HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OrganizationLeavePolicy>().HasIndex(x => new { x.OrganizationId, x.LeaveType, x.Version }).IsUnique();
        builder.Entity<OrganizationLeavePolicy>().Property(x => x.EntitlementDaysPerCycle).HasPrecision(9, 4);
        builder.Entity<LeaveRequestRecord>().HasIndex(x => new { x.EmploymentRecordId, x.Status, x.StartDate });
        builder.Entity<ContractClause>().Property(x => x.Confidence).HasPrecision(5, 4);
        builder.Entity<ContractClause>().Property(x => x.EntitlementDays).HasPrecision(9, 4);
        builder.Entity<EmploymentRecord>().Property(x => x.GrossSalary).HasPrecision(18, 2);
        builder.Entity<EmploymentRecord>().Property(x => x.WeeklyHours).HasPrecision(6, 2);
        builder.Entity<LeaveRequestRecord>().Property(x => x.ScheduledWorkdays).HasPrecision(9, 4);
        builder.Entity<LeaveLedgerRecord>().Property(x => x.DayDelta).HasPrecision(9, 4);
    }
}
