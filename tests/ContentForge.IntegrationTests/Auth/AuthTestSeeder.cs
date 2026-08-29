namespace ContentForge.IntegrationTests.Auth;

using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

internal static class AuthTestConstants
{
    internal const string AdminEmail = "admin@contentforge.test";
    internal const string AdminPassword = "AdminPassword123!";
    internal static readonly Guid AdminUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    internal const string EditorEmail = "editor@contentforge.test";
    internal const string EditorPassword = "EditorPassword123!";
    internal static readonly Guid EditorUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    internal const string AuthorEmail = "author@contentforge.test";
    internal const string AuthorPassword = "AuthorPassword123!";
    internal static readonly Guid AuthorUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    internal const string ViewerEmail = "viewer@contentforge.test";
    internal const string ViewerPassword = "ViewerPassword123!";
    internal static readonly Guid ViewerUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    internal const string DisabledEmail = "disabled@contentforge.test";
    internal const string DisabledPassword = "DisabledPassword123!";
    internal static readonly Guid DisabledUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
}

internal static class AuthTestRoleIds
{
    internal static readonly Guid Administrator = Guid.Parse("11111111-1111-1111-1111-111111111101");
    internal static readonly Guid Editor = Guid.Parse("11111111-1111-1111-1111-111111111102");
    internal static readonly Guid Author = Guid.Parse("11111111-1111-1111-1111-111111111103");
    internal static readonly Guid Viewer = Guid.Parse("11111111-1111-1111-1111-111111111104");
}

internal static class AuthTestSeeder
{
    internal static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ContentForgeUser>>();

        await EnsureUserAsync(
            userManager,
            AuthTestConstants.AdminUserId,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword,
            RoleName.Administrator,
            isActive: true);

        await EnsureUserAsync(
            userManager,
            AuthTestConstants.EditorUserId,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword,
            RoleName.Editor,
            isActive: true);

        await EnsureUserAsync(
            userManager,
            AuthTestConstants.AuthorUserId,
            AuthTestConstants.AuthorEmail,
            AuthTestConstants.AuthorPassword,
            RoleName.Author,
            isActive: true);

        await EnsureUserAsync(
            userManager,
            AuthTestConstants.ViewerUserId,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword,
            RoleName.Viewer,
            isActive: true);

        await EnsureUserAsync(
            userManager,
            AuthTestConstants.DisabledUserId,
            AuthTestConstants.DisabledEmail,
            AuthTestConstants.DisabledPassword,
            RoleName.Viewer,
            isActive: false);
    }

    private static async Task EnsureUserAsync(
        UserManager<ContentForgeUser> userManager,
        Guid userId,
        string email,
        string password,
        RoleName role,
        bool isActive)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var roleId = role switch
        {
            RoleName.Administrator => AuthTestRoleIds.Administrator,
            RoleName.Editor => AuthTestRoleIds.Editor,
            RoleName.Author => AuthTestRoleIds.Author,
            RoleName.Viewer => AuthTestRoleIds.Viewer,
            _ => throw new ArgumentOutOfRangeException(nameof(role)),
        };

        var user = new ContentForgeUser
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = true,
            DisplayName = email.Split('@')[0],
            IsActive = isActive,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UserRoles =
            [
                new UserRoleEntity
                {
                    UserId = userId,
                    RoleId = roleId,
                },
            ],
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
        }
    }
}
