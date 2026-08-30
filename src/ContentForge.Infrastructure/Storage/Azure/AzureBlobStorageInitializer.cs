namespace ContentForge.Infrastructure.BlobStorage;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Ensures the configured blob container exists when the application starts.
/// </summary>
internal sealed class AzureBlobStorageInitializer(IBlobStorageGateway gateway) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        gateway is AzureBlobStorageGateway azure
            ? azure.EnsureContainerExistsAsync(cancellationToken)
            : Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
