namespace ContentForge.Application.Abstractions.Scheduling;

using ContentForge.Domain.Common;

/// <summary>
/// Schedules deferred background work without executing it in the request thread.
/// </summary>
public interface IBackgroundJobScheduler
{
    Task ScheduleContentPublishAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset publishAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default);

    Task ScheduleContentUnpublishAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset unpublishAt,
        UserId requestedBy,
        CancellationToken cancellationToken = default);

    Task CancelContentPublishAsync(ContentEntryId contentEntryId, CancellationToken cancellationToken = default);

    Task CancelContentUnpublishAsync(ContentEntryId contentEntryId, CancellationToken cancellationToken = default);
}
