namespace ContentForge.Domain.Scheduling;

/// <summary>
/// Supported background job types for content scheduling.
/// </summary>
public enum ScheduledJobType
{
    ContentPublish = 0,
    ContentUnpublish = 1,
}
