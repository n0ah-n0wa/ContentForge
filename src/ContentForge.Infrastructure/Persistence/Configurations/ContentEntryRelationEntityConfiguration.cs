namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContentEntryRelationEntityConfiguration : IEntityTypeConfiguration<ContentEntryRelationEntity>
{
    public void Configure(EntityTypeBuilder<ContentEntryRelationEntity> builder)
    {
        builder.ToTable("ContentEntryRelations");

        builder.HasKey(relation => relation.Id);

        builder.Property(relation => relation.FieldName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(relation => relation.SourceEntryId);
        builder.HasIndex(relation => relation.TargetEntryId);
        builder.HasIndex(relation => new { relation.SourceEntryId, relation.FieldName });

        builder.HasIndex(relation => new { relation.SourceEntryId, relation.TargetEntryId, relation.FieldName })
            .IsUnique();

        builder.HasOne(relation => relation.SourceEntry)
            .WithMany(entry => entry.OutgoingRelations)
            .HasForeignKey(relation => relation.SourceEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(relation => relation.TargetEntry)
            .WithMany(entry => entry.IncomingRelations)
            .HasForeignKey(relation => relation.TargetEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
