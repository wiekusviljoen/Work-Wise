using System.ComponentModel.DataAnnotations;

namespace LeaveWise.Data;

public sealed class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(2)] public string CountryCode { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    public ICollection<EmploymentRecord> Employments { get; set; } = new List<EmploymentRecord>();
}

public sealed class OrganizationMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    [MaxLength(450)] public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    [MaxLength(32)] public string Role { get; set; } = "Employee";
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public sealed class EmploymentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    [MaxLength(450)] public string? EmployeeUserId { get; set; }
    public ApplicationUser? EmployeeUser { get; set; }
    [MaxLength(160)] public string EmployeeName { get; set; } = "";
    [MaxLength(320)] public string EmployeeEmail { get; set; } = "";
    [MaxLength(2)] public string CountryCode { get; set; } = "";
    public DateOnly EmploymentStartDate { get; set; }
    public DateOnly? EmploymentEndDate { get; set; }
    [MaxLength(3)] public string CurrencyCode { get; set; } = "NAD";
    [MaxLength(24)] public string PayFrequency { get; set; } = "Monthly";
    public decimal? GrossSalary { get; set; }
    public decimal WeeklyHours { get; set; } = 45m;
    public int ScheduledWorkdaysMask { get; set; } = 31; // Monday-Friday; bit 0 = Monday.
    [MaxLength(24)] public string Status { get; set; } = "PendingReview";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<EmploymentContract> Contracts { get; set; } = new List<EmploymentContract>();
    public ICollection<LeaveRequestRecord> LeaveRequests { get; set; } = new List<LeaveRequestRecord>();
    public ICollection<LeaveLedgerRecord> LeaveLedger { get; set; } = new List<LeaveLedgerRecord>();
    public ICollection<BenefitReminder> BenefitReminders { get; set; } = new List<BenefitReminder>();
}

public sealed class EmploymentContract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentRecordId { get; set; }
    public EmploymentRecord EmploymentRecord { get; set; } = null!;
    [MaxLength(260)] public string OriginalFileName { get; set; } = "";
    [MaxLength(500)] public string? PrivateStorageKey { get; set; }
    [MaxLength(32)] public string Status { get; set; } = "Uploaded";
    public string? ExtractedFieldsJson { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedUtc { get; set; }
    [MaxLength(450)] public string? ReviewedByUserId { get; set; }
    public ICollection<ContractClause> Clauses { get; set; } = new List<ContractClause>();
}

public sealed class ContractClause
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentContractId { get; set; }
    public EmploymentContract EmploymentContract { get; set; } = null!;
    [MaxLength(64)] public string ClauseType { get; set; } = "";
    public string ClauseText { get; set; } = "";
    public int? SourcePage { get; set; }
    public decimal? Confidence { get; set; }
    [MaxLength(32)] public string ReviewStatus { get; set; } = "NeedsReview";
    public decimal? EntitlementDays { get; set; }
}

public sealed class LeaveRequestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentRecordId { get; set; }
    public EmploymentRecord EmploymentRecord { get; set; } = null!;
    [MaxLength(48)] public string LeaveType { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? ScheduledWorkdays { get; set; }
    [MaxLength(1000)] public string? EmployeeNote { get; set; }
    [MaxLength(32)] public string Status { get; set; } = "Pending";
    [MaxLength(450)] public string RequestedByUserId { get; set; } = "";
    [MaxLength(450)] public string? ReviewedByUserId { get; set; }
    public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecisionUtc { get; set; }
}

public sealed class LeaveLedgerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentRecordId { get; set; }
    public EmploymentRecord EmploymentRecord { get; set; } = null!;
    [MaxLength(48)] public string LeaveType { get; set; } = "";
    public DateOnly EffectiveDate { get; set; }
    public decimal DayDelta { get; set; }
    [MaxLength(32)] public string EntryType { get; set; } = "";
    [MaxLength(100)] public string Reference { get; set; } = "";
    [MaxLength(1000)] public string? Notes { get; set; }
    [MaxLength(450)] public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public Guid? ReversalOfId { get; set; }
}

public sealed class BenefitReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentRecordId { get; set; }
    public EmploymentRecord EmploymentRecord { get; set; } = null!;
    [MaxLength(64)] public string BenefitType { get; set; } = "";
    [MaxLength(160)] public string Label { get; set; } = "";
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? ReminderDate { get; set; }
    [MaxLength(48)] public string EligibilityStatus { get; set; } = "NeedsReview";
    public string? FormulaOrConditionsJson { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    [MaxLength(450)] public string? ActorUserId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(80)] public string EntityType { get; set; } = "";
    [MaxLength(100)] public string EntityId { get; set; } = "";
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
    public string? DetailsJson { get; set; }
}
