namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class AuditLogEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
