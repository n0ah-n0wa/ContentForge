namespace ContentForge.Domain.Authorization;

/// <summary>
/// Well-known CMS roles.
/// </summary>
public enum RoleName
{
    Administrator,
    Editor,
    Author,
    Viewer,
}

/// <summary>
/// Represents a permission granted to users through roles.
/// </summary>
public readonly record struct PermissionName(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Canonical permission identifiers used by the authorization model.
/// </summary>
public static class Permissions
{
    public static readonly PermissionName ContentRead = new("content.read");
    public static readonly PermissionName ContentCreate = new("content.create");
    public static readonly PermissionName ContentUpdate = new("content.update");
    public static readonly PermissionName ContentDelete = new("content.delete");
    public static readonly PermissionName ContentPublish = new("content.publish");
    public static readonly PermissionName ContentArchive = new("content.archive");
    public static readonly PermissionName ContentRestore = new("content.restore");
    public static readonly PermissionName ContentReview = new("content.review");
    public static readonly PermissionName ContentVersionRead = new("content.version.read");
    public static readonly PermissionName ContentVersionRestore = new("content.version.restore");

    public static readonly PermissionName ContentTypeRead = new("contentType.read");
    public static readonly PermissionName ContentTypeCreate = new("contentType.create");
    public static readonly PermissionName ContentTypeUpdate = new("contentType.update");
    public static readonly PermissionName ContentTypeDelete = new("contentType.delete");

    public static readonly PermissionName MediaRead = new("media.read");
    public static readonly PermissionName MediaUpload = new("media.upload");
    public static readonly PermissionName MediaUpdate = new("media.update");
    public static readonly PermissionName MediaDelete = new("media.delete");

    public static readonly PermissionName UserRead = new("user.read");
    public static readonly PermissionName UserCreate = new("user.create");
    public static readonly PermissionName UserUpdate = new("user.update");
    public static readonly PermissionName UserDisable = new("user.disable");

    public static readonly PermissionName AuditRead = new("audit.read");

    public static IReadOnlyCollection<PermissionName> All { get; } =
    [
        ContentRead,
        ContentCreate,
        ContentUpdate,
        ContentDelete,
        ContentPublish,
        ContentArchive,
        ContentRestore,
        ContentReview,
        ContentVersionRead,
        ContentVersionRestore,
        ContentTypeRead,
        ContentTypeCreate,
        ContentTypeUpdate,
        ContentTypeDelete,
        MediaRead,
        MediaUpload,
        MediaUpdate,
        MediaDelete,
        UserRead,
        UserCreate,
        UserUpdate,
        UserDisable,
        AuditRead,
    ];
}

/// <summary>
/// Defines the permission set associated with a role.
/// </summary>
public sealed class RoleDefinition
{
    private RoleDefinition(RoleName name, IReadOnlySet<PermissionName> permissions)
    {
        Name = name;
        Permissions = permissions;
    }

    public RoleName Name { get; }

    public IReadOnlySet<PermissionName> Permissions { get; }

    public bool HasPermission(PermissionName permission) => Permissions.Contains(permission);

    public static RoleDefinition Create(RoleName name, IEnumerable<PermissionName> permissions)
    {
        var permissionSet = permissions.ToHashSet();
        if (permissionSet.Count == 0)
        {
            throw new Common.DomainValidationException(nameof(permissions), "A role must have at least one permission.");
        }

        return new RoleDefinition(name, permissionSet);
    }
}

/// <summary>
/// Provides the default role-to-permission mappings defined by the specification.
/// </summary>
public static class DefaultRoleDefinitions
{
    public static RoleDefinition Administrator { get; } = RoleDefinition.Create(
        RoleName.Administrator,
        Permissions.All);

    public static RoleDefinition Editor { get; } = RoleDefinition.Create(
        RoleName.Editor,
        [
            Permissions.ContentRead,
            Permissions.ContentCreate,
            Permissions.ContentUpdate,
            Permissions.ContentDelete,
            Permissions.ContentPublish,
            Permissions.ContentArchive,
            Permissions.ContentRestore,
            Permissions.ContentReview,
            Permissions.ContentVersionRead,
            Permissions.ContentVersionRestore,
            Permissions.ContentTypeRead,
            Permissions.MediaRead,
            Permissions.MediaUpload,
            Permissions.MediaUpdate,
            Permissions.MediaDelete,
        ]);

    public static RoleDefinition Author { get; } = RoleDefinition.Create(
        RoleName.Author,
        [
            Permissions.ContentRead,
            Permissions.ContentCreate,
            Permissions.ContentUpdate,
            Permissions.ContentReview,
            Permissions.ContentVersionRead,
            Permissions.ContentTypeRead,
            Permissions.MediaRead,
            Permissions.MediaUpload,
        ]);

    public static RoleDefinition Viewer { get; } = RoleDefinition.Create(
        RoleName.Viewer,
        [
            Permissions.ContentRead,
            Permissions.ContentTypeRead,
            Permissions.MediaRead,
            Permissions.ContentVersionRead,
        ]);

    public static IReadOnlyDictionary<RoleName, RoleDefinition> All { get; } =
        new Dictionary<RoleName, RoleDefinition>
        {
            [RoleName.Administrator] = Administrator,
            [RoleName.Editor] = Editor,
            [RoleName.Author] = Author,
            [RoleName.Viewer] = Viewer,
        };
}

/// <summary>
/// Domain authorization helper for permission checks.
/// </summary>
public static class AuthorizationRules
{
    public static bool IsAllowed(RoleDefinition role, PermissionName permission) =>
        role.HasPermission(permission);

    public static void EnsureAllowed(RoleDefinition role, PermissionName permission)
    {
        if (!IsAllowed(role, permission))
        {
            throw new Common.InvalidOperationDomainException(
                $"Role '{role.Name}' does not grant permission '{permission.Value}'.");
        }
    }

    public static bool CanModifyOwnContentOnly(RoleName roleName) => roleName == RoleName.Author;

    public static void EnsureCanModifyContent(
        RoleDefinition role,
        Common.UserId actorId,
        Common.UserId contentOwnerId)
    {
        if (CanModifyOwnContentOnly(role.Name) && actorId != contentOwnerId)
        {
            throw new Common.InvalidOperationDomainException(
                "Authors may only modify content they created.");
        }
    }
}
