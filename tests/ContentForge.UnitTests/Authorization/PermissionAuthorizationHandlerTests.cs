namespace ContentForge.UnitTests.Authorization;

using System.Security.Claims;
using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_Succeeds_WhenPermissionClaimPresent()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(PermissionAuthorizationHandler.PermissionClaimType, Permissions.ContentPublish.Value),
        ], authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.ContentPublish.Value)],
            user,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_Succeeds_WhenRoleGrantsPermissionWithoutClaim()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, RoleName.Editor.ToString()),
        ], authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.ContentPublish.Value)],
            user,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_Fails_WhenAuthorRequestsPublish()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, RoleName.Author.ToString()),
        ], authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.ContentPublish.Value)],
            user,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_Fails_WhenViewerRequestsContentCreate()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, RoleName.Viewer.ToString()),
        ], authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.ContentCreate.Value)],
            user,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_Fails_WhenUnauthenticated()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(Permissions.UserCreate.Value)],
            user,
            resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}

public sealed class RolePermissionMatrixTests
{
    [Theory]
    [InlineData(RoleName.Administrator, "user.create", true)]
    [InlineData(RoleName.Administrator, "content.publish", true)]
    [InlineData(RoleName.Editor, "content.publish", true)]
    [InlineData(RoleName.Editor, "user.create", false)]
    [InlineData(RoleName.Author, "content.publish", false)]
    [InlineData(RoleName.Author, "content.create", true)]
    [InlineData(RoleName.Viewer, "content.create", false)]
    [InlineData(RoleName.Viewer, "content.update", false)]
    [InlineData(RoleName.Viewer, "content.read", true)]
    public void Role_HasExpectedPermission(RoleName roleName, string permission, bool expected)
    {
        var role = DefaultRoleDefinitions.All[roleName];
        role.HasPermission(new PermissionName(permission)).Should().Be(expected);
    }

    [Fact]
    public void PermissionsCatalog_ContainsAllSpecifiedPermissions()
    {
        Permissions.All.Select(permission => permission.Value).Should().BeEquivalentTo(
        [
            "content.read",
            "content.create",
            "content.update",
            "content.delete",
            "content.publish",
            "content.archive",
            "content.restore",
            "content.review",
            "content.version.read",
            "content.version.restore",
            "contentType.read",
            "contentType.create",
            "contentType.update",
            "contentType.delete",
            "media.read",
            "media.upload",
            "media.update",
            "media.delete",
            "user.read",
            "user.create",
            "user.update",
            "user.disable",
            "audit.read",
        ]);
    }
}
