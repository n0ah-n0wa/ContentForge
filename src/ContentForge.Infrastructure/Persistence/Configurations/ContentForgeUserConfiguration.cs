namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentForgeUserConfiguration : IEntityTypeConfiguration<ContentForgeUser>
{
    public void Configure(EntityTypeBuilder<ContentForgeUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(user => user.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(320);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.CreatedAt);
    }
}
