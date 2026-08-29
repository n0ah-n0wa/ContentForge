namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceVersionImmutability();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public override int SaveChanges()
    {
        EnforceVersionImmutability();
        return base.SaveChanges();
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureProviderSpecificIndexes(modelBuilder);
    }

    private void ConfigureProviderSpecificIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentEntryEntity>()
            .HasIndex(entry => new { entry.ContentTypeId, entry.Slug })
            .IsUnique()
            .HasFilter(DatabaseIndexFilters.SoftDelete(Database.IsNpgsql()));
    }

    private void EnforceVersionImmutability()
    {
        foreach (var entry in ChangeTracker.Entries<ContentVersionEntity>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Content versions are append-only and cannot be modified or deleted.");
            }
        }
    }
}
