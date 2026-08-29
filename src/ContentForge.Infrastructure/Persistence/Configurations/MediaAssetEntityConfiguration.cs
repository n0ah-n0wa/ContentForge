namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MediaAssetEntityConfiguration : IEntityTypeConfiguration<MediaAssetEntity>
{
    public void Configure(EntityTypeBuilder<MediaAssetEntity> builder)
    {
        builder.ToTable("Media");

        builder.HasKey(media => media.Id);

        builder.Property(media => media.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(media => media.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(media => media.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(media => media.StorageKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(media => media.Url)
            .HasMaxLength(2048);

        builder.Property(media => media.AltText)
            .HasMaxLength(500);

        builder.Property(media => media.Title)
            .HasMaxLength(200);

        builder.Property(media => media.Description)
            .HasMaxLength(2000);

        builder.HasIndex(media => media.UploadedAt);
        builder.HasIndex(media => media.IsDeleted);
        builder.HasIndex(media => media.UploadedBy);
        builder.HasIndex(media => media.ContentType);
    }
}
