namespace ContentForge.Application.Scheduling;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Application.Common;
using ContentForge.Application.Media;
using ContentForge.Application.Scheduling.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class ScheduledJobProcessor(
    IScheduledJobRepository jobRepository,
    IContentEntryRepository contentEntryRepository,
    IContentTypeRepository contentTypeRepository,
    IMediaRepository mediaRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider clock,
    IOptions<ScheduledPublishingOptions> options,
    ILogger<ScheduledJobProcessor> logger) : IScheduledJobProcessor
{
    private readonly ScheduledPublishingOptions _options = options.Value;
    private readonly string _instanceId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public async Task<int> ProcessDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        await jobRepository.ReleaseStaleLocksAsync(now, cancellationToken).ConfigureAwait(false);

        var jobs = await jobRepository.ClaimDueJobsAsync(
            now,
            _instanceId,
            TimeSpan.FromSeconds(_options.LockDurationSeconds),
            _options.BatchSize,
            cancellationToken).ConfigureAwait(false);

        if (jobs.Count == 0)
        {
            return 0;
        }

        ScheduledJobProcessorLogger.ClaimedJobs(logger, jobs.Count, _instanceId);

        var processed = 0;
        foreach (var job in jobs)
        {
            try
            {
                await ExecuteJobAsync(job, cancellationToken).ConfigureAwait(false);
                await jobRepository.MarkCompletedAsync(job.Id, clock.UtcNow, cancellationToken).ConfigureAwait(false);
                processed++;
            }
            catch (Exception ex)
            {
                ScheduledJobProcessorLogger.JobFailed(logger, ex, job.Id, job.JobType, job.AttemptCount);

                var shouldRetry = job.AttemptCount < _options.MaxAttempts;
                var nextAttempt = shouldRetry
                    ? clock.UtcNow.AddSeconds(_options.RetryDelaySeconds * job.AttemptCount)
                    : (DateTimeOffset?)null;

                await jobRepository.MarkFailedAsync(
                    job.Id,
                    ex.Message,
                    clock.UtcNow,
                    nextAttempt,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return processed;
    }

    private async Task ExecuteJobAsync(ScheduledJob job, CancellationToken cancellationToken)
    {
        var entryId = ContentEntryId.From(job.ContentEntryId);
        var entry = await contentEntryRepository.GetByIdAsync(entryId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Content entry '{job.ContentEntryId}' was not found.");

        var actorId = UserId.From(job.RequestedBy);
        var timestamp = clock.UtcNow;

        switch (job.JobType)
        {
            case ScheduledJobType.ContentPublish:
                await ExecutePublishAsync(entry, actorId, timestamp, cancellationToken).ConfigureAwait(false);
                break;
            case ScheduledJobType.ContentUnpublish:
                await ExecuteUnpublishAsync(entry, actorId, timestamp, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unsupported scheduled job type '{job.JobType}'.");
        }
    }

    private async Task ExecutePublishAsync(
        ContentEntry entry,
        UserId actorId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (entry.IsAlreadyPublishedForSchedule())
        {
            ScheduledJobProcessorLogger.SkippingPublishAlreadyPublished(logger, entry.Id.Value);
            entry.ClearScheduledPublish(actorId, timestamp);
            await contentEntryRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var contentType = await contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Content type '{entry.ContentTypeId.Value}' was not found.");

        await MediaReferenceValidator.ValidateAsync(contentType, entry.DraftData, mediaRepository, cancellationToken)
            .ConfigureAwait(false);

        entry.PublishScheduled(contentType, actorId, timestamp);
        await contentEntryRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditService.RecordAsync(
            AuditAction.ContentPublished,
            "ContentEntry",
            entry.Id.Value.ToString(),
            actorId,
            metadata: AuditMetadataSanitizer.Build(("source", "scheduled")),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteUnpublishAsync(
        ContentEntry entry,
        UserId actorId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (entry.IsAlreadyUnpublishedForSchedule())
        {
            ScheduledJobProcessorLogger.SkippingUnpublishAlreadyUnpublished(logger, entry.Id.Value);
            entry.ClearScheduledUnpublish(actorId, timestamp);
            await contentEntryRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        entry.UnpublishScheduled(actorId, timestamp);
        await contentEntryRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditService.RecordAsync(
            AuditAction.ContentUnpublished,
            "ContentEntry",
            entry.Id.Value.ToString(),
            actorId,
            metadata: AuditMetadataSanitizer.Build(("source", "scheduled")),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
