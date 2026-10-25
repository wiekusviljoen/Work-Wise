using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LeaveWise.Data;

namespace LeaveWise.Domain;

public sealed class EmployerAccountRequirement : IAuthorizationRequirement;

public sealed class EmployerAccountAuthorizationHandler(IDbContextFactory<ApplicationDbContext> factory)
    : AuthorizationHandler<EmployerAccountRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmployerAccountRequirement requirement)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) return;
        await using var db = await factory.CreateDbContextAsync();
        var access = await AccountAccessService.GetAsync(db, id);
        if (access.CanUseEmployerWorkspace) context.Succeed(requirement);
    }
}
