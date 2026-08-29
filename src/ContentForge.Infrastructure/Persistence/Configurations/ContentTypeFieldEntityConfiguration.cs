namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentTypeFieldEntityConfiguration : IEntityTypeConfiguration<ContentTypeFieldEntity>
{
    public void Configure(EntityTypeBuilder<ContentTypeFieldEntity> builder)
    {
        builder.ToTable("ContentTypeFields");

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(field => field.FieldType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(field => field.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(field => field.ConfigurationJson)
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(field => new { field.ContentTypeId, field.Name })
            .IsUnique();

        builder.HasIndex(field => new { field.ContentTypeId, field.SortOrder });
    }
}
