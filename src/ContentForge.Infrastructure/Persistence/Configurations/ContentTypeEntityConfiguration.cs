namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentTypeEntityConfiguration : IEntityTypeConfiguration<ContentTypeEntity>
{
    public void Configure(EntityTypeBuilder<ContentTypeEntity> builder)
    {
        builder.ToTable("ContentTypes");

        builder.HasKey(contentType => contentType.Id);

        builder.Property(contentType => contentType.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(contentType => contentType.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(contentType => contentType.Description)
            .HasMaxLength(2000);

        builder.Property(contentType => contentType.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(contentType => contentType.Name)
            .IsUnique();

        builder.HasIndex(contentType => contentType.Slug)
            .IsUnique();

        builder.HasIndex(contentType => contentType.IsActive);
        builder.HasIndex(contentType => contentType.CreatedAt);
        builder.HasIndex(contentType => contentType.UpdatedAt);

        builder.HasMany(contentType => contentType.Fields)
            .WithOne(field => field.ContentType)
            .HasForeignKey(field => field.ContentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
