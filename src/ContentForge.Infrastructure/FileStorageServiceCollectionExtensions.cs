namespace ContentForge.Infrastructure;

using Azure.Storage.Blobs;
using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Storage;
using ContentForge.Infrastructure.BlobStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

internal static class FileStorageServiceCollectionExtensions
{
    internal static IServiceCollection AddFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        services.Configure<MediaStorageOptions>(configuration.GetSection(MediaStorageOptions.SectionName));

        var provider = ResolveProvider(
            configuration.GetSection(MediaStorageOptions.SectionName).Get<MediaStorageOptions>()?.Provider,
            environment);

        switch (provider.ToUpperInvariant())
        {
            case "INMEMORY":
                services.AddSingleton<IFileStorage, InMemoryFileStorage>();
                break;
            case "AZURE":
                RegisterAzureBlobStorage(services);
                break;
            default:
                services.AddSingleton<IFileStorage, LocalFileStorage>();
                break;
        }

        return services;
    }

    private static void RegisterAzureBlobStorage(IServiceCollection services)
    {
        services.AddSingleton<BlobContainerClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MediaStorageOptions>>().Value;
            AzureBlobClientFactory.ValidateAzureOptions(options);
            return AzureBlobClientFactory.CreateContainerClient(options);
        });
        services.AddSingleton<IBlobStorageGateway, AzureBlobStorageGateway>();
        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        services.AddHostedService<AzureBlobStorageInitializer>();
    }

    private static string ResolveProvider(string? configured, IHostEnvironment? environment)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        if (environment?.IsProduction() == true)
        {
            return "Azure";
        }

        return "Local";
    }
}
