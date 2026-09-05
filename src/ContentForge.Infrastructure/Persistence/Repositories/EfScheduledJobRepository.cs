namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Application.Scheduling.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.Scheduling;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfScheduledJobRepository(AppDbContext dbContext) : IScheduledJobRepository
{
    public async Task UpsertPendingJobAsync(
        ContentEntryId contentEntryId,
        ScheduledJobType jobType,
        DateTimeOffset scheduledAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await CancelPendingJobsAsync(contentEntryId, jobType, cancellationToken).ConfigureAwait(false);

        var idempotencyKey = BuildIdempotencyKey(contentEntryId, jobType, scheduledAt);
        var entity = new ScheduledJobEntity
        {
            Id = Guid.NewGuid(),
            ContentEntryId = contentEntryId.Value,
            JobType = jobType.ToString(),
            ScheduledAt = scheduledAt,
            Status = ScheduledJobStatus.Pending.ToString(),
            IdempotencyKey = idempotencyKey,
            RequestedBy = requestedBy.Value,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.ScheduledJobs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelPendingJobsAsync(
        ContentEntryId contentEntryId,
        ScheduledJobType jobType,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var pendingJobs = await dbContext.ScheduledJobs
            .Where(job =>
                job.ContentEntryId == contentEntryId.Value
                && job.JobType == jobType.ToString()
                && job.Status == ScheduledJobStatus.Pending.ToString())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var job in pendingJobs)
        {
            job.Status = ScheduledJobStatus.Cancelled.ToString();
            job.UpdatedAt = now;
            job.CompletedAt = now;
        }

        if (pendingJobs.Count > 0)
        {
            // Must flush before ContentEntry UpdateAsync clears the change tracker.
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<ScheduledJob>> ClaimDueJobsAsync(
        DateTimeOffset now,
        string instanceId,
        TimeSpan lockDuration,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var pendingStatus = ScheduledJobStatus.Pending.ToString();
        var dueJobIds = await ClaimDueJobIdsAsync(pendingStatus, now, batchSize, cancellationToken)
            .ConfigureAwait(false);

        if (dueJobIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return [];
        }

        var lockedUntil = now.Add(lockDuration);
        var entities = await dbContext.ScheduledJobs
            .Where(job => dueJobIds.Contains(job.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var entity in entities)
        {
            entity.Status = ScheduledJobStatus.Running.ToString();
            entity.LockedUntil = lockedUntil;
            entity.LockedBy = instanceId;
            entity.AttemptCount += 1;
            entity.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return entities.Select(ScheduledJobMapper.ToDomain).ToList();
    }

    public async Task MarkCompletedAsync(Guid jobId, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ScheduledJobs.SingleAsync(job => job.Id == jobId, cancellationToken).ConfigureAwait(false);
        entity.Status = ScheduledJobStatus.Completed.ToString();
        entity.CompletedAt = completedAt;
        entity.LockedUntil = null;
        entity.LockedBy = null;
        entity.LastError = null;
        entity.UpdatedAt = completedAt;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(
        Guid jobId,
        string errorMessage,
        DateTimeOffset failedAt,
        DateTimeOffset? nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ScheduledJobs.SingleAsync(job => job.Id == jobId, cancellationToken).ConfigureAwait(false);
        entity.Status = nextAttemptAt is null
            ? ScheduledJobStatus.Failed.ToString()
            : ScheduledJobStatus.Pending.ToString();
        entity.LastError = errorMessage.Length <= 2048 ? errorMessage : errorMessage[..2048];
        entity.NextAttemptAt = nextAttemptAt;
        entity.LockedUntil = null;
        entity.LockedBy = null;
        entity.UpdatedAt = failedAt;
        if (nextAttemptAt is null)
        {
            entity.CompletedAt = failedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkCancelledAsync(Guid jobId, DateTimeOffset cancelledAt, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ScheduledJobs.SingleAsync(job => job.Id == jobId, cancellationToken).ConfigureAwait(false);
        entity.Status = ScheduledJobStatus.Cancelled.ToString();
        entity.CompletedAt = cancelledAt;
        entity.UpdatedAt = cancelledAt;
        entity.LockedUntil = null;
        entity.LockedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseStaleLocksAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var staleJobs = await dbContext.ScheduledJobs
            .Where(job =>
                job.Status == ScheduledJobStatus.Running.ToString()
                && job.LockedUntil != null
                && job.LockedUntil < now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var job in staleJobs)
        {
            job.Status = ScheduledJobStatus.Pending.ToString();
            job.LockedUntil = null;
            job.LockedBy = null;
            job.UpdatedAt = now;
        }

        if (staleJobs.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<List<Guid>> ClaimDueJobIdsAsync(
        string pendingStatus,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return await dbContext.Database
                .SqlQueryRaw<Guid>(
                    """
                    SELECT "Id"
                    FROM "ScheduledJobs"
                    WHERE "Status" = {0}
                      AND "ScheduledAt" <= {1}
                      AND ("NextAttemptAt" IS NULL OR "NextAttemptAt" <= {1})
                      AND ("LockedUntil" IS NULL OR "LockedUntil" < {1})
                    ORDER BY "ScheduledAt"
                    LIMIT {2}
                    FOR UPDATE SKIP LOCKED
                    """,
                    pendingStatus,
                    now,
                    batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return await dbContext.Database
                .SqlQueryRaw<Guid>(
                    """
                    SELECT TOP ({2}) Id
                    FROM ScheduledJobs WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE Status = {0}
                      AND ScheduledAt <= {1}
                      AND (NextAttemptAt IS NULL OR NextAttemptAt <= {1})
                      AND (LockedUntil IS NULL OR LockedUntil < {1})
                    ORDER BY ScheduledAt
                    """,
                    pendingStatus,
                    now,
                    batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        throw new NotSupportedException(
            $"Scheduled job claiming is not supported for database provider '{providerName}'.");
    }

    private static string BuildIdempotencyKey(ContentEntryId contentEntryId, ScheduledJobType jobType, DateTimeOffset scheduledAt) =>
        $"{contentEntryId.Value:D}:{jobType}:{scheduledAt.ToUniversalTime():O}";
}

internal static class ScheduledJobMapper
{
    internal static ScheduledJob ToDomain(ScheduledJobEntity entity) =>
        new(
            entity.Id,
            entity.ContentEntryId,
            Enum.Parse<ScheduledJobType>(entity.JobType),
            entity.ScheduledAt,
            Enum.Parse<ScheduledJobStatus>(entity.Status),
            entity.IdempotencyKey,
            entity.RequestedBy,
            entity.AttemptCount,
            entity.NextAttemptAt,
            entity.LockedUntil,
            entity.LockedBy,
            entity.CompletedAt,
            entity.LastError,
            entity.CreatedAt,
            entity.UpdatedAt);
}
