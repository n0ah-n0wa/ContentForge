namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentEntryEntityConfiguration : IEntityTypeConfiguration<ContentEntryEntity>
{
    public void Configure(EntityTypeBuilder<ContentEntryEntity> builder)
    {
        builder.ToTable("ContentEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entry => entry.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entry => entry.DraftDataJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(entry => entry.PublishedSnapshotJson)
            .HasColumnType("text");

        builder.Property(entry => entry.ConcurrencyToken)
            .IsConcurrencyToken();

        builder.HasIndex(entry => entry.ContentTypeId);
        builder.HasIndex(entry => entry.Status);
        builder.HasIndex(entry => entry.Slug);
        builder.HasIndex(entry => entry.CreatedAt);
        builder.HasIndex(entry => entry.UpdatedAt);
        builder.HasIndex(entry => entry.IsDeleted);
        builder.HasIndex(entry => entry.CreatedBy);
        builder.HasIndex(entry => entry.ScheduledPublishAt);
        builder.HasIndex(entry => entry.ScheduledUnpublishAt);

        builder.HasIndex(entry => new { entry.ContentTypeId, entry.IsDeleted, entry.UpdatedAt });

        builder.HasOne(entry => entry.ContentType)
            .WithMany(contentType => contentType.Entries)
            .HasForeignKey(entry => entry.ContentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entry => entry.Versions)
            .WithOne(version => version.ContentEntry)
            .HasForeignKey(version => version.ContentEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
