using Microsoft.EntityFrameworkCore;
using LeaveWise.Data;

namespace LeaveWise.Domain;

/// <summary>Account intent is stored as an Identity claim, never trusted from the login form.</summary>
public static class AccountAccessService
{
    public const string AccountTypeClaim = "WorkWise.AccountType";
    public const string Employer = "Employer";
    public const string Employee = "Employee";

    public sealed record Access(bool CanUseEmployerWorkspace, bool IsOwner, bool IsEmployee);

    public static async Task<Access> GetAsync(ApplicationDbContext db, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return new(false, false, false);

        var owner = await db.OrganizationMemberships.AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.Role == "Owner", cancellationToken);
        var employeeMembership = await db.OrganizationMemberships.AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.Role == "Employee", cancellationToken);
        var linkedEmployment = await db.EmploymentRecords.AnyAsync(x =>
            x.EmployeeUserId == userId && x.Status == "Active", cancellationToken);
        var declaredRole = await db.UserClaims.Where(x => x.UserId == userId && x.ClaimType == AccountTypeClaim)
            .Select(x => x.ClaimValue).FirstOrDefaultAsync(cancellationToken);

        // Existing owners are preserved when upgrading from accounts without an account-type claim.
        // Declaring "Employer" does not elevate a linked employee to employer privileges.
        // Legacy accounts without an explicit role are treated as employees until associated with an owner workspace.
        // This keeps invitation redemption available without granting employer setup rights.
        var isEmployee = employeeMembership || linkedEmployment || declaredRole == Employee
            || (declaredRole is null && !owner);
        var canUseEmployerWorkspace = owner || (declaredRole == Employer && !isEmployee);
        return new(canUseEmployerWorkspace, owner, isEmployee);
    }
}
