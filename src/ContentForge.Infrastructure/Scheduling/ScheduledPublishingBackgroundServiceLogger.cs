namespace ContentForge.Infrastructure.Scheduling;

using Microsoft.Extensions.Logging;

internal static partial class ScheduledPublishingBackgroundServiceLogger
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Scheduled publishing background worker started with poll interval {PollIntervalSeconds}s")]
    public static partial void WorkerStarted(ILogger logger, int pollIntervalSeconds);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Processed {ProcessedCount} scheduled job(s)")]
    public static partial void ProcessedJobs(ILogger logger, int processedCount);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "Scheduled publishing worker iteration failed")]
    public static partial void WorkerIterationFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Scheduled publishing background worker stopped")]
    public static partial void WorkerStopped(ILogger logger);
}
