namespace ContentForge.Application.Abstractions.Scheduling;

/// <summary>
/// Executes due scheduled jobs. Intended for background workers and integration tests.
/// </summary>
public interface IScheduledJobProcessor
{
    Task<int> ProcessDueJobsAsync(CancellationToken cancellationToken = default);
}
