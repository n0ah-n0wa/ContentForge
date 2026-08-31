namespace ContentForge.Application.Scheduling.Models;

using ContentForge.Domain.Scheduling;

/// <summary>
/// Persisted scheduled job returned by repositories and processors.
/// </summary>
public sealed record ScheduledJob(
    Guid Id,
    Guid ContentEntryId,
    ScheduledJobType JobType,
    DateTimeOffset ScheduledAt,
    ScheduledJobStatus Status,
    string IdempotencyKey,
    Guid RequestedBy,
    int AttemptCount,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? LockedUntil,
    string? LockedBy,
    DateTimeOffset? CompletedAt,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
