namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(role => role.Name)
            .IsUnique();

        builder.HasData(
            CreateRole(RoleName.Administrator),
            CreateRole(RoleName.Editor),
            CreateRole(RoleName.Author),
            CreateRole(RoleName.Viewer));
    }

    private static RoleEntity CreateRole(RoleName roleName) => new()
    {
        Id = AuthorizationSeedIds.RoleId(roleName),
        Name = roleName.ToString(),
    };
}
