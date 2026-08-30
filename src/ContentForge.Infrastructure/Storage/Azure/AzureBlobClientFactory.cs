namespace ContentForge.Infrastructure.BlobStorage;

using Azure.Identity;
using Azure.Storage.Blobs;
using ContentForge.Infrastructure.Options;
using Microsoft.Extensions.Options;

internal static class AzureBlobClientFactory
{
    internal static BlobContainerClient CreateContainerClient(MediaStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateAzureOptions(options);

        var serviceClient = CreateServiceClient(options);
        return serviceClient.GetBlobContainerClient(options.ContainerName);
    }

    internal static void ValidateAzureOptions(MediaStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("Media:ContainerName must be configured for Azure blob storage.");
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return;
        }

        if (!options.UseManagedIdentity)
        {
            throw new InvalidOperationException(
                "Azure blob storage requires Media:ConnectionString or Media:UseManagedIdentity=true with Media:BlobServiceUri or Media:StorageAccountName.");
        }

        if (string.IsNullOrWhiteSpace(options.BlobServiceUri) && string.IsNullOrWhiteSpace(options.StorageAccountName))
        {
            throw new InvalidOperationException(
                "Managed identity blob access requires Media:BlobServiceUri or Media:StorageAccountName.");
        }
    }

    private static BlobServiceClient CreateServiceClient(MediaStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        var serviceUri = ResolveServiceUri(options);
        return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
    }

    private static Uri ResolveServiceUri(MediaStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BlobServiceUri))
        {
            return new Uri(options.BlobServiceUri, UriKind.Absolute);
        }

        return new Uri($"https://{options.StorageAccountName}.blob.core.windows.net", UriKind.Absolute);
    }
}
