using LeaveWise.Data;
using LeaveWise.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Tests;

public class EmployeeManagementTests
{
    [Fact]
    public async Task OwnerCanEditAndArchive_ButOthersCannot_AndHistorySurvives()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var org = new Organization { Name = "Company", CountryCode = "NA" };
        db.Users.AddRange(new ApplicationUser { Id = "owner", Email = "boss@example.test" }, new ApplicationUser { Id = "staff", Email = "staff@example.test" }, new ApplicationUser { Id = "outsider" });
        db.OrganizationMemberships.AddRange(new OrganizationMembership { Organization = org, UserId = "owner", Role = "Owner" }, new OrganizationMembership { Organization = org, UserId = "staff", Role = "Employee" });
        var employee = new EmploymentRecord { Organization = org, EmployeeName = "Original", EmployeeEmail = "staff@example.test", EmploymentStartDate = new(2026, 1, 1), Status = "Active" };
        db.EmploymentRecords.Add(employee);
        await db.SaveChangesAsync();
        var edit = new EmployeeEditInput { EmployeeName = "Updated Employee", EmployeeEmail = employee.EmployeeEmail, EmploymentStartDate = employee.EmploymentStartDate, WeeklyHours = 40 };
        Assert.NotNull(await EmployeeManagementService.SaveAsync(db, employee.Id, "outsider", edit));
        Assert.NotNull(await EmployeeManagementService.SaveAsync(db, employee.Id, "staff", null));
        Assert.Equal("Original", employee.EmployeeName);
        var invitation = await EmploymentInviteService.CreateInviteAsync(db, employee.Id, "owner", DateTime.UtcNow);
        Assert.True(invitation.Succeeded);
        Assert.Null(await EmployeeManagementService.SaveAsync(db, employee.Id, "owner", edit));
        db.ChangeTracker.Clear();
        employee = await db.EmploymentRecords.SingleAsync();
        Assert.Equal("Updated Employee", employee.EmployeeName);
        Assert.Equal(40m, employee.WeeklyHours);
        Assert.False((await EmploymentInviteService.RedeemInviteAsync(db, "staff", invitation.Code!, DateTime.UtcNow)).Succeeded);
        employee.EmployeeUserId = "staff";
        db.LeaveRequests.Add(new LeaveRequestRecord { EmploymentRecord = employee, RequestedByUserId = "staff", Status = "Pending", StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2) });
        await db.SaveChangesAsync();
        edit.EmployeeEmail = "different@example.test";
        Assert.NotNull(await EmployeeManagementService.SaveAsync(db, employee.Id, "owner", edit));
        edit.EmployeeEmail = employee.EmployeeEmail;
        edit.EmploymentStartDate = new(2026, 2, 1);
        Assert.NotNull(await EmployeeManagementService.SaveAsync(db, employee.Id, "owner", edit));
        Assert.Null(await EmployeeManagementService.SaveAsync(db, employee.Id, "owner", null));
        db.ChangeTracker.Clear();
        Assert.Equal("Archived", (await db.EmploymentRecords.SingleAsync()).Status);
        Assert.Equal(1, await db.LeaveRequests.CountAsync());
        Assert.False((await db.OrganizationMemberships.SingleAsync(x => x.UserId == "staff")).IsActive);
        Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == "EmploymentRecordUpdated"));
        Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == "EmploymentRecordArchived"));
        Assert.False((await EmploymentInviteService.CreateInviteAsync(db, employee.Id, "owner", DateTime.UtcNow)).Succeeded);
    }
}
