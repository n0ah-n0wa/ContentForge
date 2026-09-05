namespace ContentForge.Application.Scheduling;

using ContentForge.Domain.Scheduling;
using Microsoft.Extensions.Logging;

internal static partial class ScheduledJobProcessorLogger
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Claimed {JobCount} scheduled job(s) for processing on instance {InstanceId}")]
    public static partial void ClaimedJobs(ILogger logger, int jobCount, string instanceId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Scheduled job {JobId} ({JobType}) failed on attempt {AttemptCount}")]
    public static partial void JobFailed(
        ILogger logger,
        Exception exception,
        Guid jobId,
        ScheduledJobType jobType,
        int attemptCount);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Skipping scheduled publish for content entry {ContentEntryId} because it is already published.")]
    public static partial void SkippingPublishAlreadyPublished(ILogger logger, Guid contentEntryId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Skipping scheduled unpublish for content entry {ContentEntryId} because it is already unpublished.")]
    public static partial void SkippingUnpublishAlreadyUnpublished(ILogger logger, Guid contentEntryId);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Warning,
        Message = "Skipping scheduled publish for content entry {ContentEntryId} because no publish schedule is set.")]
    public static partial void SkippingPublishNoSchedule(ILogger logger, Guid contentEntryId);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Warning,
        Message = "Skipping scheduled unpublish for content entry {ContentEntryId} because no unpublish schedule is set.")]
    public static partial void SkippingUnpublishNoSchedule(ILogger logger, Guid contentEntryId);
}
