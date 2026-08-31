namespace ContentForge.IntegrationTests.Api;

using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Domain.Scheduling;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static class ScheduledPublishingTestHelper
{
    internal static async Task<int> ProcessDueJobsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IScheduledJobProcessor>();
        return await processor.ProcessDueJobsAsync(cancellationToken);
    }

    internal static async Task MakeJobsDueAsync(IServiceProvider services, Guid contentEntryId, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dueAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await dbContext.ScheduledJobs
            .Where(job =>
                job.ContentEntryId == contentEntryId
                && job.Status == ScheduledJobStatus.Pending.ToString())
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(job => job.ScheduledAt, dueAt),
                cancellationToken);
    }

    internal static async Task<IReadOnlyList<ScheduledJobEntity>> GetJobsAsync(
        IServiceProvider services,
        Guid contentEntryId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.ScheduledJobs
            .AsNoTracking()
            .Where(job => job.ContentEntryId == contentEntryId)
            .OrderBy(job => job.JobType)
            .ToListAsync(cancellationToken);
    }
}
