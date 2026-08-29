namespace ContentForge.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Requires a specific ContentForge permission claim (or equivalent role grant).
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
