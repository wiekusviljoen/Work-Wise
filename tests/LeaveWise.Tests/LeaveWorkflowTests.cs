using LeaveWise.Data;
using LeaveWise.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Tests;

public sealed class LeaveWorkflowTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private ApplicationDbContext db = null!;
    private EmploymentRecord employee = null!;
    private readonly DateOnly today = new(2026, 10, 26);
    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var org = new Organization { Name = "QA", CountryCode = "NA" };
        db.Users.AddRange(new ApplicationUser { Id = "boss" }, new ApplicationUser { Id = "staff" }, new ApplicationUser { Id = "other" });
        db.OrganizationMemberships.Add(new OrganizationMembership { Organization = org, UserId = "boss", Role = "Owner" });
        employee = new EmploymentRecord { Organization = org, EmployeeName = "QA", EmployeeEmail = "qa@example.invalid", EmployeeUserId = "staff", Status = "Active", EmploymentStartDate = new(2026,1,1) };
        db.EmploymentRecords.Add(employee);
        await db.SaveChangesAsync();
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    private Task<LeaveActionResult> Submit(DateOnly start, DateOnly? end = null, string type = "Unpaid leave", string actor = "staff") => LeaveRequestService.SubmitAsync(db, actor, employee.Id, type, start, end ?? start, null, today);

    [Fact] public async Task DuplicateIsRejected_CancellationAllowsReplacement_AndOtherUserCannotCancel()
    {
        var first = await Submit(today);
        Assert.True(first.Succeeded);
        Assert.False((await Submit(today)).Succeeded);
        Assert.False((await LeaveRequestService.CancelAsync(db, "other", first.Request!.Id)).Succeeded);
        Assert.True((await LeaveRequestService.CancelAsync(db, "staff", first.Request!.Id)).Succeeded);
        Assert.True((await Submit(today)).Succeeded);
        Assert.Equal(2, await db.LeaveRequests.CountAsync());
    }
    [Fact] public async Task InvalidDatesInactiveRecordAndWrongUserAreRejected()
    {
        Assert.False((await Submit(today, actor:"other")).Succeeded);
        Assert.False((await Submit(today.AddDays(-1))).Succeeded);
        Assert.False((await Submit(today.AddDays(5))).Succeeded); // Saturday
        employee.EmploymentStartDate = today.AddDays(1);
        await db.SaveChangesAsync();
        Assert.False((await Submit(today)).Succeeded);
        employee.EmploymentStartDate = today;
        employee.EmploymentEndDate = today;
        await db.SaveChangesAsync();
        Assert.False((await Submit(today.AddDays(1))).Succeeded);
        employee.Status = "Archived";
        await db.SaveChangesAsync();
        Assert.False((await Submit(today)).Succeeded);
    }
    [Fact] public async Task ApprovalChecksOwnerAndConfirmation_AndDecisionCannotBeOverwritten()
    {
        var saved = (await Submit(today)).Request!;
        Assert.False((await LeaveRequestService.DecideAsync(db,"other",saved.Id,true,1,true,today)).Succeeded);
        Assert.False((await LeaveRequestService.DecideAsync(db,"boss",saved.Id,true,1,false,today)).Succeeded);
        Assert.True((await LeaveRequestService.DecideAsync(db,"boss",saved.Id,true,1,true,today)).Succeeded);
        Assert.False((await LeaveRequestService.DecideAsync(db,"boss",saved.Id,false,1,true,today)).Succeeded);
        Assert.False((await LeaveRequestService.CancelAsync(db,"staff",saved.Id)).Succeeded);
        Assert.Equal("Approved",saved.Status);
        Assert.Empty(await db.LeaveLedger.ToListAsync());
    }
    [Fact] public async Task PaidApprovalReservesLaterApprovedLeaveBeforeEarlierRequests()
    {
        db.OrganizationLeavePolicies.Add(new OrganizationLeavePolicy { OrganizationId=employee.OrganizationId, LeaveType="Annual leave", EffectiveFrom=new(2026,10,1), CycleMonths=12, EntitlementDaysPerCycle=12, Status="Active" });
        db.LeaveLedger.Add(new LeaveLedgerRecord { EmploymentRecord=employee, LeaveType="Annual leave", DayDelta=1, EffectiveDate=today, EntryType="Opening", Reference="test" });
        await db.SaveChangesAsync();
        var later=(await Submit(today.AddDays(7),type:"Annual leave")).Request!;
        Assert.True((await LeaveRequestService.DecideAsync(db,"boss",later.Id,true,1,true,today)).Succeeded);
        var earlier=(await Submit(today,type:"Annual leave")).Request!;
        Assert.False((await LeaveRequestService.DecideAsync(db,"boss",earlier.Id,true,1,true,today)).Succeeded);
        Assert.Equal("Pending",earlier.Status);
        Assert.Equal(0,await LeaveLedgerService.GetAvailableBalanceAsync(db,employee.Id,"Annual leave",today));
    }
    [Fact] public async Task AccrualStopsAtEmploymentEnd()
    {
        employee.EmploymentEndDate = new(2026,2,28);
        db.OrganizationLeavePolicies.Add(new OrganizationLeavePolicy { OrganizationId=employee.OrganizationId, LeaveType="Annual leave", EffectiveFrom=new(2026,1,1), CycleMonths=12, EntitlementDaysPerCycle=12, Status="Active" });
        await db.SaveChangesAsync();
        Assert.Equal(2,await LeaveLedgerService.SyncAccrualsAsync(db,employee,today,"boss"));
    }
    [Fact] public async Task StaleDecisionCannotOverwriteCancelledStatus()
    {
        var saved=(await Submit(today)).Request!;
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var staleDb=new ApplicationDbContext(options);
        var stale=await staleDb.LeaveRequests.SingleAsync(x=>x.Id==saved.Id);
        Assert.True((await LeaveRequestService.CancelAsync(db,"staff",saved.Id)).Succeeded);
        stale.Status="Approved";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>staleDb.SaveChangesAsync());
    }
}
