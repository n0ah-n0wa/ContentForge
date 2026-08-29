namespace ContentForge.Infrastructure.Persistence.Seed;

using ContentForge.Domain.Authorization;

/// <summary>
/// Deterministic identifiers for seeded authorization data.
/// </summary>
internal static class AuthorizationSeedIds
{
    internal static readonly Guid AdministratorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    internal static readonly Guid EditorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    internal static readonly Guid AuthorRoleId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    internal static readonly Guid ViewerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111104");

    internal static Guid PermissionId(PermissionName permission)
    {
        var index = Permissions.All
            .Select((value, position) => new { value, position })
            .Single(item => item.value.Value == permission.Value)
            .position;

        return Guid.Parse($"22222222-2222-2222-2222-{(index + 1):D12}");
    }

    internal static Guid RoleId(RoleName roleName) => roleName switch
    {
        RoleName.Administrator => AdministratorRoleId,
        RoleName.Editor => EditorRoleId,
        RoleName.Author => AuthorRoleId,
        RoleName.Viewer => ViewerRoleId,
        _ => throw new ArgumentOutOfRangeException(nameof(roleName)),
    };
}
