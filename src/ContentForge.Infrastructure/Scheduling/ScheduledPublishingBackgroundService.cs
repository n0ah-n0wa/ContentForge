namespace ContentForge.Infrastructure.Scheduling;

using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Application.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class ScheduledPublishingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledPublishingOptions> options,
    ILogger<ScheduledPublishingBackgroundService> logger) : BackgroundService
{
    private readonly ScheduledPublishingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ScheduledPublishingBackgroundServiceLogger.WorkerStarted(logger, _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IScheduledJobProcessor>();
                var processed = await processor.ProcessDueJobsAsync(stoppingToken).ConfigureAwait(false);

                if (processed > 0)
                {
                    ScheduledPublishingBackgroundServiceLogger.ProcessedJobs(logger, processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                ScheduledPublishingBackgroundServiceLogger.WorkerIterationFailed(logger, ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        ScheduledPublishingBackgroundServiceLogger.WorkerStopped(logger);
    }
}
