namespace ContentForge.Infrastructure.Persistence.Configurations;

using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(log => log.EntityType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(log => log.EntityId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(log => log.Metadata)
            .HasColumnType("text");

        builder.Property(log => log.IpAddress)
            .HasMaxLength(64);

        builder.Property(log => log.UserAgent)
            .HasMaxLength(512);

        builder.HasIndex(log => log.Timestamp);
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => new { log.EntityType, log.EntityId });
    }
}
