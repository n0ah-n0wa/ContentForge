namespace ContentForge.Infrastructure.Health;

using Azure.Storage.Blobs;
using ContentForge.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Verifies the configured media storage backend is reachable and usable.
/// </summary>
internal sealed class MediaStorageHealthCheck(
    IOptions<MediaStorageOptions> options,
    IServiceProvider serviceProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = options.Value.Provider.Trim().ToUpperInvariant();
        return provider switch
        {
            "INMEMORY" => HealthCheckResult.Healthy(),
            "AZURE" => await CheckAzureAsync(cancellationToken).ConfigureAwait(false),
            _ => CheckLocal(options.Value),
        };
    }

    private static HealthCheckResult CheckLocal(MediaStorageOptions options)
    {
        try
        {
            var root = Path.GetFullPath(options.LocalRoot);
            Directory.CreateDirectory(root);

            var probePath = Path.Combine(root, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);
            return HealthCheckResult.Healthy();
        }
        catch (IOException)
        {
            return HealthCheckResult.Unhealthy("Storage is unavailable.");
        }
        catch (UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("Storage is unavailable.");
        }
    }

    private async Task<HealthCheckResult> CheckAzureAsync(CancellationToken cancellationToken)
    {
        var containerClient = serviceProvider.GetService<BlobContainerClient>();
        if (containerClient is null)
        {
            return HealthCheckResult.Unhealthy("Storage is unavailable.");
        }

        try
        {
            var exists = await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            return exists.Value
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Storage is unavailable.");
        }
        catch (IOException)
        {
            return HealthCheckResult.Unhealthy("Storage is unavailable.");
        }
        catch (UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("Storage is unavailable.");
        }
    }
}
