using LeaveWise.Data;
using LeaveWise.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LeaveWise.Tests;

public sealed class AccountAccessTests
{
    [Fact]
    public async Task EmployeeAndOwnerAreSeparated_AndOwnerCanFindEmployeePendingLeave()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var company = new Organization { Name = "Test Company", CountryCode = "NA" };
        db.Users.AddRange(
            new ApplicationUser { Id = "boss", UserName = "boss@example.test", Email = "boss@example.test" },
            new ApplicationUser { Id = "staff", UserName = "staff@example.test", Email = "staff@example.test" });
        db.Organizations.Add(company);
        db.OrganizationMemberships.AddRange(
            new OrganizationMembership { Organization = company, UserId = "boss", Role = "Owner", IsActive = true },
            new OrganizationMembership { Organization = company, UserId = "staff", Role = "Employee", IsActive = true });
        var employment = new EmploymentRecord
        {
            Organization = company, EmployeeUserId = "staff", EmployeeEmail = "staff@example.test",
            EmployeeName = "Employee", CountryCode = "NA", EmploymentStartDate = new DateOnly(2026, 10, 1), Status = "Active"
        };
        db.EmploymentRecords.Add(employment);
        db.LeaveRequests.Add(new LeaveRequestRecord
        {
            EmploymentRecord = employment, RequestedByUserId = "staff", LeaveType = "Annual leave",
            StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 3), Status = "Pending"
        });
        await db.SaveChangesAsync();

        var employer = await AccountAccessService.GetAsync(db, "boss");
        var employee = await AccountAccessService.GetAsync(db, "staff");
        Assert.True(employer.CanUseEmployerWorkspace);
        Assert.True(employer.IsOwner);
        Assert.False(employee.CanUseEmployerWorkspace);
        Assert.True(employee.IsEmployee);
        var assignedPending = await db.LeaveRequests.CountAsync(x =>
            x.Status == "Pending" && db.OrganizationMemberships.Any(m =>
                m.UserId == "boss" && m.Role == "Owner" && m.IsActive &&
                m.OrganizationId == x.EmploymentRecord.OrganizationId));
        Assert.Equal(1, assignedPending);
    }

    [Fact]
    public async Task DeclaredEmployerCanSetUpNewCompany_ButEmployeeClaimCannotEscalate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Users.AddRange(new ApplicationUser { Id = "newBoss" }, new ApplicationUser { Id = "newStaff" });
        db.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "newBoss", ClaimType = AccountAccessService.AccountTypeClaim, ClaimValue = AccountAccessService.Employer },
            new IdentityUserClaim<string> { UserId = "newStaff", ClaimType = AccountAccessService.AccountTypeClaim, ClaimValue = AccountAccessService.Employee });
        await db.SaveChangesAsync();

        Assert.True((await AccountAccessService.GetAsync(db, "newBoss")).CanUseEmployerWorkspace);
        Assert.False((await AccountAccessService.GetAsync(db, "newStaff")).CanUseEmployerWorkspace);
        Assert.True((await AccountAccessService.GetAsync(db, "newStaff")).IsEmployee);
        db.Users.Add(new ApplicationUser { Id = "legacyAccount" });
        await db.SaveChangesAsync();
        var legacyAccess = await AccountAccessService.GetAsync(db, "legacyAccount");
        Assert.True(legacyAccess.IsEmployee);
        Assert.False(legacyAccess.CanUseEmployerWorkspace);
    }
}
