namespace ContentForge.Application.Abstractions.Scheduling;

using ContentForge.Application.Scheduling.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.Scheduling;

/// <summary>
/// Persistence port for scheduled background jobs.
/// </summary>
public interface IScheduledJobRepository
{
    Task UpsertPendingJobAsync(
        ContentEntryId contentEntryId,
        ScheduledJobType jobType,
        DateTimeOffset scheduledAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default);

    Task CancelPendingJobsAsync(
        ContentEntryId contentEntryId,
        ScheduledJobType jobType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduledJob>> ClaimDueJobsAsync(
        DateTimeOffset now,
        string instanceId,
        TimeSpan lockDuration,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(Guid jobId, DateTimeOffset completedAt, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid jobId,
        string errorMessage,
        DateTimeOffset failedAt,
        DateTimeOffset? nextAttemptAt,
        CancellationToken cancellationToken = default);

    Task MarkCancelledAsync(Guid jobId, DateTimeOffset cancelledAt, CancellationToken cancellationToken = default);

    Task ReleaseStaleLocksAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
