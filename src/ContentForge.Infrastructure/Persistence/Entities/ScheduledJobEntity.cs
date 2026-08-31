namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ScheduledJobEntity
{
    public Guid Id { get; set; }

    public Guid ContentEntryId { get; set; }

    public string JobType { get; set; } = null!;

    public DateTimeOffset ScheduledAt { get; set; }

    public string Status { get; set; } = null!;

    public string IdempotencyKey { get; set; } = null!;

    public Guid RequestedBy { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public string? LockedBy { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
