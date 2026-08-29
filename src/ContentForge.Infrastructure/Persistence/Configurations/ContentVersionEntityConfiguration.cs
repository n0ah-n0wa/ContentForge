namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentVersionEntityConfiguration : IEntityTypeConfiguration<ContentVersionEntity>
{
    public void Configure(EntityTypeBuilder<ContentVersionEntity> builder)
    {
        builder.ToTable("ContentVersions");

        builder.HasKey(version => version.Id);

        builder.Property(version => version.SnapshotJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(version => version.ChangeSummary)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(version => version.ContentEntryId);
        builder.HasIndex(version => version.CreatedAt);

        builder.HasIndex(version => new { version.ContentEntryId, version.VersionNumber })
            .IsUnique();
    }
}
