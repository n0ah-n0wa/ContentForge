namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PermissionEntityConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(permission => permission.Name)
            .IsUnique();

        builder.HasData(
            Permissions.All.Select(permission => new PermissionEntity
            {
                Id = AuthorizationSeedIds.PermissionId(permission),
                Name = permission.Value,
            }));
    }
}
