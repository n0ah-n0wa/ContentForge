namespace ContentForge.IntegrationTests.Persistence;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class MigrationPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task Migrations_CreateExpectedTablesAndSeedAuthorizationData()
    {
        await using var scope = CreatePersistenceScope();

        await scope.DbContext.Users.CountAsync();
        await scope.DbContext.Roles.CountAsync();
        await scope.DbContext.Permissions.CountAsync();
        await scope.DbContext.RolePermissions.CountAsync();
        await scope.DbContext.UserRoles.CountAsync();
        await scope.DbContext.ContentTypes.CountAsync();
        await scope.DbContext.ContentTypeFields.CountAsync();
        await scope.DbContext.ContentEntries.CountAsync();
        await scope.DbContext.ContentVersions.CountAsync();
        await scope.DbContext.ContentEntryRelations.CountAsync();
        await scope.DbContext.MediaAssets.CountAsync();
        await scope.DbContext.AuditLogs.CountAsync();

        var roleCount = await scope.DbContext.Roles.CountAsync();
        var permissionCount = await scope.DbContext.Permissions.CountAsync();
        var rolePermissionCount = await scope.DbContext.RolePermissions.CountAsync();

        roleCount.Should().Be(4);
        permissionCount.Should().Be(23);
        rolePermissionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Migrations_HaveNoPendingModelChanges()
    {
        await using var scope = CreatePersistenceScope();

        var pendingMigrations = await scope.DbContext.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }
}
