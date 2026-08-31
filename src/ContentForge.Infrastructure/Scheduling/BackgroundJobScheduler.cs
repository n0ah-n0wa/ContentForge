namespace ContentForge.Infrastructure.Scheduling;

using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Domain.Common;
using ContentForge.Domain.Scheduling;

internal sealed class BackgroundJobScheduler(IScheduledJobRepository repository) : IBackgroundJobScheduler
{
    public Task ScheduleContentPublishAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset publishAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default) =>
        repository.UpsertPendingJobAsync(
            contentEntryId,
            ScheduledJobType.ContentPublish,
            publishAt,
            requestedBy,
            cancellationToken);

    public Task ScheduleContentUnpublishAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset unpublishAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default) =>
        repository.UpsertPendingJobAsync(
            contentEntryId,
            ScheduledJobType.ContentUnpublish,
            unpublishAt,
            requestedBy,
            cancellationToken);

    public Task CancelContentPublishAsync(ContentEntryId contentEntryId, CancellationToken cancellationToken = default) =>
        repository.CancelPendingJobsAsync(contentEntryId, ScheduledJobType.ContentPublish, cancellationToken);

    public Task CancelContentUnpublishAsync(ContentEntryId contentEntryId, CancellationToken cancellationToken = default) =>
        repository.CancelPendingJobsAsync(contentEntryId, ScheduledJobType.ContentUnpublish, cancellationToken);
}
