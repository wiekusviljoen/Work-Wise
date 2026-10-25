using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LeaveWise.Data;

namespace LeaveWise.Domain;

public sealed class EmployerAuthorizationHandler(IDbContextFactory<ApplicationDbContext> factory)
    : AuthorizationHandler<EmployerRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmployerRequirement requirement)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) return;
        await using var db = await factory.CreateDbContextAsync();
        if (await db.OrganizationMemberships.AnyAsync(x => x.UserId == id && x.IsActive && x.Role == "Owner"))
            context.Succeed(requirement);
    }
}

public sealed class EmployerRequirement : IAuthorizationRequirement;
