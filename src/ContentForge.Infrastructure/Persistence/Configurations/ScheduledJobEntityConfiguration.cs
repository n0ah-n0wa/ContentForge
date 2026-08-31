namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ScheduledJobEntityConfiguration : IEntityTypeConfiguration<ScheduledJobEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledJobEntity> builder)
    {
        builder.ToTable("ScheduledJobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.JobType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(job => job.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(job => job.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(job => job.LockedBy)
            .HasMaxLength(128);

        builder.Property(job => job.LastError)
            .HasMaxLength(2048);

        builder.HasIndex(job => job.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(job => new { job.Status, job.ScheduledAt });
        builder.HasIndex(job => new { job.ContentEntryId, job.JobType, job.Status });
        builder.HasIndex(job => job.LockedUntil);
    }
}
