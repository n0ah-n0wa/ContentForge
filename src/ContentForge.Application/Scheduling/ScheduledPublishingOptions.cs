namespace ContentForge.Application.Scheduling;

/// <summary>
/// Configuration for scheduled publishing background processing.
/// </summary>
public sealed class ScheduledPublishingOptions
{
    public const string SectionName = "ScheduledPublishing";

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int LockDurationSeconds { get; set; } = 120;

    public int MaxAttempts { get; set; } = 5;

    public int RetryDelaySeconds { get; set; } = 30;
}
