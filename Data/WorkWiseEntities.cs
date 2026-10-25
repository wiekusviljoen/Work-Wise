namespace LeaveWise.Data;

public sealed class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string CountryCode { get; set; } = "NA";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
}

public sealed class OrganizationMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = "Employee";
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string CountryCode { get; set; } = "NA";
    public DateOnly EmploymentStartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string CurrencyCode { get; set; } = "NAD";
    public decimal? GrossMonthlySalary { get; set; }
    public int OrdinaryDaysPerWeek { get; set; } = 5;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmploymentContractRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile EmployeeProfile { get; set; } = null!;
    public string OriginalFileName { get; set; } = "";
    public string? PrivateStorageKey { get; set; }
    public string Status { get; set; } = "PendingReview";
    public string? ExtractedTermsJson { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }
}

public sealed class LeaveRequestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile EmployeeProfile { get; set; } = null!;
    public string LeaveType { get; set; } = "Annual";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? RequestedWorkingDays { get; set; }
    public string Status { get; set; } = "Pending";
    public string? EmployeeNote { get; set; }
    public string? ReviewerNote { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class LeaveTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile EmployeeProfile { get; set; } = null!;
    public string LeaveType { get; set; } = "Annual";
    public decimal QuantityDays { get; set; }
    public string TransactionType { get; set; } = "Adjustment";
    public DateOnly EffectiveDate { get; set; }
    public string Source { get; set; } = "Manual";
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = "";
}

public sealed class BenefitReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile EmployeeProfile { get; set; } = null!;
    public string BenefitType { get; set; } = "13thCheque";
    public string Title { get; set; } = "";
    public string? ContractClause { get; set; }
    public string? EligibilityConditions { get; set; }
    public DateOnly? ExpectedPaymentDate { get; set; }
    public decimal? EstimatedAmount { get; set; }
    public string Status { get; set; } = "NeedsReview";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string ActorUserId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? DetailsJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
