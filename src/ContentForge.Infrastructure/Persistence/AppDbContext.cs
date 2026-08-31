namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : IdentityUserContext<
    ContentForgeUser,
    Guid,
    IdentityUserClaim<Guid>,
    IdentityUserLogin<Guid>,
    IdentityUserToken<Guid>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceImmutabilityGuards();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public override int SaveChanges()
    {
        EnforceImmutabilityGuards();
        return base.SaveChanges();
    }

    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();

    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();

    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    public DbSet<ContentTypeEntity> ContentTypes => Set<ContentTypeEntity>();

    public DbSet<ContentTypeFieldEntity> ContentTypeFields => Set<ContentTypeFieldEntity>();

    public DbSet<ContentEntryEntity> ContentEntries => Set<ContentEntryEntity>();

    public DbSet<ContentVersionEntity> ContentVersions => Set<ContentVersionEntity>();

    public DbSet<ContentEntryRelationEntity> ContentEntryRelations => Set<ContentEntryRelationEntity>();

    public DbSet<MediaAssetEntity> MediaAssets => Set<MediaAssetEntity>();

    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    public DbSet<ScheduledJobEntity> ScheduledJobs => Set<ScheduledJobEntity>();

    public DbSet<ContentPreviewTokenEntity> ContentPreviewTokens => Set<ContentPreviewTokenEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureIdentityTables(builder);
        ConfigureProviderSpecificIndexes(builder);
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }

    private void ConfigureProviderSpecificIndexes(ModelBuilder builder)
    {
        builder.Entity<ContentEntryEntity>()
            .HasIndex(entry => new { entry.ContentTypeId, entry.Slug })
            .IsUnique()
            .HasFilter(DatabaseIndexFilters.SoftDelete(Database.IsNpgsql()));
    }

    private void EnforceImmutabilityGuards()
    {
        EnforceVersionImmutability();
        EnforceAuditLogImmutability();
    }

    private void EnforceVersionImmutability()
    {
        var deletedEntryIds = ChangeTracker.Entries<ContentEntryEntity>()
            .Where(entry => entry.State == EntityState.Deleted)
            .Select(entry => entry.Entity.Id)
            .ToHashSet();

        foreach (var entry in ChangeTracker.Entries<ContentVersionEntity>())
        {
            if (entry.State == EntityState.Modified
                || (entry.State == EntityState.Deleted && !deletedEntryIds.Contains(entry.Entity.ContentEntryId)))
            {
                throw new InvalidOperationException("Content versions are append-only and cannot be modified or deleted.");
            }
        }
    }

    private void EnforceAuditLogImmutability()
    {
        foreach (var entry in ChangeTracker.Entries<AuditLogEntity>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit log entries are immutable and cannot be modified or deleted.");
            }
        }
    }
}
