using LeaveWise.Domain;
using LeaveWise.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Tests;

public sealed class LeaveCalculatorTests
{
    private static readonly IReadOnlySet<DayOfWeek> Weekdays = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    [Fact]
    public void CountScheduledWorkdays_CountsInclusiveWeekdaysAndExcludesHoliday()
    {
        var holiday = new HashSet<DateOnly> { new(2026, 10, 7) };
        var result = LeaveCalculator.CountScheduledWorkdays(new(2026, 10, 5), new(2026, 10, 9), Weekdays, holiday);
        Assert.Equal(4, result);
    }

    [Fact]
    public void CountScheduledWorkdays_RejectsReversedRange()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveCalculator.CountScheduledWorkdays(new(2026, 10, 10), new(2026, 10, 9), Weekdays));
    }

    [Fact]
    public void CountScheduledWorkdays_HandlesMaximumDateWithoutOverflow()
    {
        var everyDay = Enum.GetValues<DayOfWeek>().ToHashSet();
        Assert.Equal(1, LeaveCalculator.CountScheduledWorkdays(DateOnly.MaxValue, DateOnly.MaxValue, everyDay));
    }

    [Fact]
    public void CountScheduledWorkdays_RejectsEmptySchedule()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveCalculator.CountScheduledWorkdays(new(2026, 10, 5), new(2026, 10, 5), new HashSet<DayOfWeek>()));
    }

    [Fact]
    public void CalculateMonthlyAccrual_UsesConfiguredEntitlementAndCapsAtCycle()
    {
        var policy = new LeaveCalculationPolicy("TEST-01", "XX", 12, 24m, Weekdays);
        Assert.Equal(12m, LeaveCalculator.CalculateMonthlyAccrual(policy, 6));
        Assert.Equal(24m, LeaveCalculator.CalculateMonthlyAccrual(policy, 18));
    }

    [Fact]
    public void CalculateBalance_ReconstructsClosingBalanceFromLedger()
    {
        var entries = new[]
        {
            new LeaveLedgerEntry(new(2026, 10, 1), 2m, "accrual", "A-1"),
            new LeaveLedgerEntry(new(2026, 10, 5), -1.5m, "usage", "L-1"),
            new LeaveLedgerEntry(new(2026, 10, 6), 0.5m, "adjustment", "ADJ-1")
        };
        var result = LeaveCalculator.CalculateBalance(10m, entries, new(2026, 10, 1), new(2026, 10, 31), Weekdays);
        Assert.Equal(2m, result.AccruedDays);
        Assert.Equal(1.5m, result.UsedDays);
        Assert.Equal(0.5m, result.Adjustments);
        Assert.Equal(11m, result.ClosingBalance);
        Assert.Contains("closing balance 11", result.Explanation);
    }
}

public sealed class LeavePolicyCatalogTests
{
    [Fact]
    public void CatalogContainsBothCountriesAndEveryEntryRequiresLegalReview()
    {
        Assert.Contains(LeavePolicyCatalog.All, x => x.CountryCode == "NA");
        Assert.Contains(LeavePolicyCatalog.All, x => x.CountryCode == "ZA");
        Assert.All(LeavePolicyCatalog.All, x => Assert.True(x.RequiresLegalReview));
        Assert.All(LeavePolicyCatalog.All, x => Assert.False(string.IsNullOrWhiteSpace(x.SourceUrl)));
    }

    [Fact]
    public void NamibiaCatalogDistinguishesCompassionateLeaveFromSouthAfricanFamilyResponsibilityLeave()
    {
        Assert.Contains(LeavePolicyCatalog.ForCountry("NA"), x => x.LeaveType == "Compassionate leave");
        Assert.Contains(LeavePolicyCatalog.ForCountry("ZA"), x => x.LeaveType == "Family responsibility leave");
    }

    [Fact]
    public void SouthAfricanParentalLeaveCatalogReferencesConstitutionalCourtInterimFramework()
    {
        var rule = Assert.Single(LeavePolicyCatalog.ForCountry("ZA"), x => x.LeaveType == "Parental leave");
        Assert.Contains("2025", rule.LegalReference);
        Assert.Contains("interim", rule.BaselineSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(rule.RequiresLegalReview);
    }
}


public sealed class LeaveLedgerServiceTests
{
    [Fact]
    public async Task SyncAccruals_IsIdempotentAndPostsMonthlyAnniversaries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var organization = new Organization { Name = "Test Org", CountryCode = "NA" };
        var employment = new EmploymentRecord
        {
            Organization = organization, EmployeeName = "Test Employee", EmployeeEmail = "test@example.invalid",
            CountryCode = "NA", EmploymentStartDate = new DateOnly(2026, 1, 15), Status = "Active"
        };
        db.EmploymentRecords.Add(employment);
        db.OrganizationLeavePolicies.Add(new OrganizationLeavePolicy
        {
            Organization = organization, LeaveType = "Annual leave", Version = 1,
            EffectiveFrom = new DateOnly(2026, 1, 1), CycleMonths = 12, EntitlementDaysPerCycle = 12m,
            AccrualMethod = "MonthlyFixed", Status = "Active", ReviewReference = "Reviewed contract clause",
            CreatedByUserId = "owner-1", ActivatedByUserId = "owner-1"
        });
        await db.SaveChangesAsync();

        var firstRun = await LeaveLedgerService.SyncAccrualsAsync(db, employment, new DateOnly(2026, 4, 30), "owner-1");
        var secondRun = await LeaveLedgerService.SyncAccrualsAsync(db, employment, new DateOnly(2026, 4, 30), "owner-1");
        var balance = await LeaveLedgerService.GetBalanceAsync(db, employment.Id, "Annual leave", new DateOnly(2026, 4, 30));

        Assert.Equal(3, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Equal(3m, balance);
        Assert.Equal(3, await db.LeaveLedger.CountAsync());
        Assert.Equal(1, await db.AuditEvents.CountAsync(x => x.Action == "LeaveAccrualsPosted"));
    }

    [Fact]
    public async Task SyncAccruals_RespectsSupersededPolicyEndDate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var organization = new Organization { Name = "Test Org", CountryCode = "ZA" };
        var employment = new EmploymentRecord
        {
            Organization = organization, EmployeeName = "Test Employee", EmployeeEmail = "test@example.invalid",
            CountryCode = "ZA", EmploymentStartDate = new DateOnly(2026, 1, 1), Status = "Active"
        };
        db.EmploymentRecords.Add(employment);
        db.OrganizationLeavePolicies.AddRange(
            new OrganizationLeavePolicy
            {
                Organization = organization, LeaveType = "Annual leave", Version = 1,
                EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = new DateOnly(2026, 3, 31),
                CycleMonths = 12, EntitlementDaysPerCycle = 12m, AccrualMethod = "MonthlyFixed",
                Status = "Superseded", ReviewReference = "Previous reviewed policy", CreatedByUserId = "owner-1"
            },
            new OrganizationLeavePolicy
            {
                Organization = organization, LeaveType = "Annual leave", Version = 2,
                EffectiveFrom = new DateOnly(2026, 4, 1), CycleMonths = 12, EntitlementDaysPerCycle = 24m,
                AccrualMethod = "MonthlyFixed", Status = "Active", ReviewReference = "New reviewed policy",
                CreatedByUserId = "owner-1", ActivatedByUserId = "owner-1"
            });
        await db.SaveChangesAsync();

        var posted = await LeaveLedgerService.SyncAccrualsAsync(db, employment, new DateOnly(2026, 5, 31), "owner-1");
        var entries = await db.LeaveLedger.OrderBy(x => x.EffectiveDate).ToListAsync();

        Assert.Equal(5, posted);
        Assert.Collection(entries,
            x => { Assert.Equal(new DateOnly(2026, 1, 31), x.EffectiveDate); Assert.Equal(1m, x.DayDelta); },
            x => { Assert.Equal(new DateOnly(2026, 2, 28), x.EffectiveDate); Assert.Equal(1m, x.DayDelta); },
            x => { Assert.Equal(new DateOnly(2026, 3, 31), x.EffectiveDate); Assert.Equal(1m, x.DayDelta); },
            x => { Assert.Equal(new DateOnly(2026, 4, 30), x.EffectiveDate); Assert.Equal(2m, x.DayDelta); },
            x => { Assert.Equal(new DateOnly(2026, 5, 31), x.EffectiveDate); Assert.Equal(2m, x.DayDelta); });
    }
}

public sealed class LeaveLedgerRoundingTests
{
    [Fact]
    public async Task SyncAccruals_TrueUpsRoundingAtCycleEnd()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var organization = new Organization { Name = "Rounding Test", CountryCode = "ZA" };
        var employment = new EmploymentRecord
        {
            Organization = organization, EmployeeName = "Test Employee", EmployeeEmail = "rounding@example.invalid",
            CountryCode = "ZA", EmploymentStartDate = new DateOnly(2026, 1, 1), Status = "Active"
        };
        db.EmploymentRecords.Add(employment);
        db.OrganizationLeavePolicies.Add(new OrganizationLeavePolicy
        {
            Organization = organization, LeaveType = "Annual leave", Version = 1,
            EffectiveFrom = new DateOnly(2026, 1, 1), CycleMonths = 12, EntitlementDaysPerCycle = 20m,
            AccrualMethod = "MonthlyFixed", Status = "Active", ReviewReference = "Reviewed contract clause",
            CreatedByUserId = "owner-1"
        });
        await db.SaveChangesAsync();

        await LeaveLedgerService.SyncAccrualsAsync(db, employment, new DateOnly(2026, 12, 31), "owner-1");
        var balance = await LeaveLedgerService.GetBalanceAsync(db, employment.Id, "Annual leave", new DateOnly(2026, 12, 31));

        Assert.Equal(20m, balance);
        Assert.Equal(12, await db.LeaveLedger.CountAsync());
    }
}

public sealed class EmploymentInviteServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Invitation_CanBeRedeemedByMatchingUnconfirmedAccount_OnlyOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employmentId = await SeedInviteScenarioAsync(db);

        var invite = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "owner-1", Now);
        Assert.True(invite.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(invite.Code));

        var created = await db.AuditEvents.SingleAsync(x => x.Action == "EmployeeInviteCreated");
        Assert.DoesNotContain(invite.Code!, created.DetailsJson);

        var redeemed = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", invite.Code!, Now.AddMinutes(1));
        Assert.True(redeemed.Succeeded);

        var replay = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", invite.Code!, Now.AddMinutes(2));
        Assert.False(replay.Succeeded);

        var employment = await db.EmploymentRecords.AsNoTracking().SingleAsync(x => x.Id == employmentId);
        Assert.Equal("employee-1", employment.EmployeeUserId);
        var membership = await db.OrganizationMemberships.AsNoTracking()
            .SingleAsync(x => x.UserId == "employee-1" && x.OrganizationId == employment.OrganizationId);
        Assert.Equal("Employee", membership.Role);
        Assert.True(membership.IsActive);
        Assert.Equal(1, await db.AuditEvents.CountAsync(x => x.Action == "EmployeeInviteRedeemed"));
    }

    [Fact]
    public async Task CreatingANewInvitationRevokesThePreviousCode()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employmentId = await SeedInviteScenarioAsync(db);

        var first = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "owner-1", Now);
        var second = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "owner-1", Now.AddMinutes(1));
        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);

        var oldCode = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", first.Code!, Now.AddMinutes(2));
        Assert.False(oldCode.Succeeded);

        var currentCode = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", second.Code!, Now.AddMinutes(2));
        Assert.True(currentCode.Succeeded);
    }

    [Fact]
    public async Task InvitationRejectsWrongEmailWithoutConsumingCode()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employmentId = await SeedInviteScenarioAsync(db);

        var invite = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "owner-1", Now);
        var wrongUser = await EmploymentInviteService.RedeemInviteAsync(db, "other-1", invite.Code!, Now.AddMinutes(1));
        Assert.False(wrongUser.Succeeded);

        var intendedUser = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", invite.Code!, Now.AddMinutes(2));
        Assert.True(intendedUser.Succeeded);
    }

    [Fact]
    public async Task InvitationExpiresAfterSevenDays()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employmentId = await SeedInviteScenarioAsync(db);

        var invite = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "owner-1", Now);
        var result = await EmploymentInviteService.RedeemInviteAsync(db, "employee-1", invite.Code!, Now.AddDays(7).AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Contains("invalid, expired, or already used", result.Message);
    }

    [Fact]
    public async Task OnlyAnActiveOrganizationOwnerCanCreateAnInvitation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var employmentId = await SeedInviteScenarioAsync(db);

        var result = await EmploymentInviteService.CreateInviteAsync(db, employmentId, "other-1", Now);

        Assert.False(result.Succeeded);
        Assert.Null(result.Code);
        Assert.Empty(await db.AuditEvents.Where(x => x.Action == "EmployeeInviteCreated").ToListAsync());
    }

    private static async Task<Guid> SeedInviteScenarioAsync(ApplicationDbContext db)
    {
        db.Users.AddRange(
            new ApplicationUser { Id = "owner-1", UserName = "owner@example.invalid", NormalizedUserName = "OWNER@EXAMPLE.INVALID", Email = "owner@example.invalid", NormalizedEmail = "OWNER@EXAMPLE.INVALID", EmailConfirmed = true },
            new ApplicationUser { Id = "employee-1", UserName = "employee@example.invalid", NormalizedUserName = "EMPLOYEE@EXAMPLE.INVALID", Email = "employee@example.invalid", NormalizedEmail = "EMPLOYEE@EXAMPLE.INVALID", EmailConfirmed = false },
            new ApplicationUser { Id = "other-1", UserName = "other@example.invalid", NormalizedUserName = "OTHER@EXAMPLE.INVALID", Email = "other@example.invalid", NormalizedEmail = "OTHER@EXAMPLE.INVALID", EmailConfirmed = false });
        var organization = new Organization { Name = "Invitation Test", CountryCode = "NA" };
        db.Organizations.Add(organization);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            Organization = organization, UserId = "owner-1", Role = "Owner", IsActive = true
        });
        var employment = new EmploymentRecord
        {
            Organization = organization, EmployeeName = "Test Employee", EmployeeEmail = "employee@example.invalid",
            CountryCode = "NA", EmploymentStartDate = new DateOnly(2026, 1, 1), Status = "Active"
        };
        db.EmploymentRecords.Add(employment);
        await db.SaveChangesAsync();
        return employment.Id;
    }
}
