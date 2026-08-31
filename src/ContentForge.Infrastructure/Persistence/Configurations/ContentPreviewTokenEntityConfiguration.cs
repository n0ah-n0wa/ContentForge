namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentPreviewTokenEntityConfiguration : IEntityTypeConfiguration<ContentPreviewTokenEntity>
{
    public void Configure(EntityTypeBuilder<ContentPreviewTokenEntity> builder)
    {
        builder.ToTable("ContentPreviewTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => new { token.ContentEntryId, token.RevokedAt, token.ExpiresAt });
    }
}
