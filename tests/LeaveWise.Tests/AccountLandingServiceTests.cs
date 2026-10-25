using LeaveWise.Domain;

namespace LeaveWise.Tests;

public sealed class AccountLandingServiceTests
{
    private static readonly AccountAccessService.Access Employee = new(false, false, true);
    private static readonly AccountAccessService.Access Owner = new(true, true, false);
    private static readonly AccountAccessService.Access NewEmployer = new(true, false, false);

    [Theory]
    [InlineData(null, "leave")]
    [InlineData("", "leave")]
    [InlineData("/", "leave")]
    [InlineData("/workspace", "leave")]
    [InlineData("/approvals", "leave")]
    [InlineData("/policies?organization=1", "leave")]
    [InlineData("/team", "leave")]
    [InlineData("/Account/AccessDenied", "leave")]
    [InlineData("/Account/Login?returnUrl=%2Fapprovals", "leave")]
    [InlineData("https://example.com/leave", "leave")]
    [InlineData("//example.com/leave", "leave")]
    [InlineData("/leave", "/leave")]
    [InlineData("contracts", "contracts")]
    [InlineData("/benefits?tab=history", "/benefits?tab=history")]
    [InlineData("/Account/Manage", "/Account/Manage")]
    public void EmployeeNeverGetsStaleOrEmployerOnlyReturnUrl(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AccountLandingService.Resolve(Employee, returnUrl));
    }

    [Theory]
    [InlineData(null, "approvals")]
    [InlineData("/", "approvals")]
    [InlineData("/approvals", "/approvals")]
    [InlineData("/workspace", "/workspace")]
    [InlineData("/team", "/team")]
    [InlineData("/policies", "/policies")]
    [InlineData("/Account/AccessDenied", "approvals")]
    public void OwnerCanReturnToEmployerPages(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AccountLandingService.Resolve(Owner, returnUrl));
    }

    [Theory]
    [InlineData(null, "workspace")]
    [InlineData("/workspace", "/workspace")]
    [InlineData("/team", "/team")]
    [InlineData("/approvals", "workspace")]
    [InlineData("/policies", "workspace")]
    public void NewEmployerCannotGoToOwnerOnlyPages(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AccountLandingService.Resolve(NewEmployer, returnUrl));
    }
}
