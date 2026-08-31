namespace ContentForge.Domain.Scheduling;

/// <summary>
/// Lifecycle states for persisted background jobs.
/// </summary>
public enum ScheduledJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}
