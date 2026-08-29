namespace ContentForge.Application.Authorization;

using ContentForge.Domain.Authorization;

/// <summary>
/// ASP.NET authorization policy names aligned with the domain permission catalog.
/// Controllers reference these constants; Infrastructure registers matching policies.
/// </summary>
public static class AuthorizationPolicies
{
    public const string ContentRead = "content.read";
    public const string ContentCreate = "content.create";
    public const string ContentUpdate = "content.update";
    public const string ContentDelete = "content.delete";
    public const string ContentPublish = "content.publish";
    public const string ContentArchive = "content.archive";
    public const string ContentRestore = "content.restore";
    public const string ContentReview = "content.review";
    public const string ContentVersionRead = "content.version.read";
    public const string ContentVersionRestore = "content.version.restore";

    public const string ContentTypeRead = "contentType.read";
    public const string ContentTypeCreate = "contentType.create";
    public const string ContentTypeUpdate = "contentType.update";
    public const string ContentTypeDelete = "contentType.delete";

    public const string MediaRead = "media.read";
    public const string MediaUpload = "media.upload";
    public const string MediaUpdate = "media.update";
    public const string MediaDelete = "media.delete";

    public const string UserRead = "user.read";
    public const string UserCreate = "user.create";
    public const string UserUpdate = "user.update";
    public const string UserDisable = "user.disable";

    public const string AuditRead = "audit.read";

    public static IReadOnlyList<string> All { get; } =
        Permissions.All.Select(permission => permission.Value).ToArray();
}
