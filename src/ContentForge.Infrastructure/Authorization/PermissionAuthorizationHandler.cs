namespace ContentForge.Infrastructure.Authorization;

using System.Security.Claims;
using ContentForge.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Evaluates permission policies using JWT permission claims, falling back to role → permission mapping.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(PermissionClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrWhiteSpace(roleClaim)
            && Enum.TryParse<RoleName>(roleClaim, out var roleName)
            && DefaultRoleDefinitions.All.TryGetValue(roleName, out var role)
            && role.HasPermission(new PermissionName(requirement.Permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
