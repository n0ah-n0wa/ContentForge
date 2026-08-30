namespace ContentForge.IntegrationTests.Storage;

using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.BlobStorage;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

/// <summary>
/// Optional integration coverage against the Azurite emulator started by docker compose.
/// </summary>
public sealed class AzuriteBlobStorageIntegrationTests
{
    // Well-known Azurite development key documented by Microsoft; not a production secret.
    private const string _azuriteConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    [Fact]
    public async Task AzureBlobStorage_UploadOpenDelete_WorksAgainstAzurite()
    {
        if (!await IsAzuriteAvailableAsync())
        {
            return;
        }

        var options = new MediaStorageOptions
        {
            ContainerName = $"media-tests-{Guid.NewGuid():N}",
            ConnectionString = _azuriteConnectionString,
            PublicBaseUrl = "http://127.0.0.1:10000/devstoreaccount1/media",
        };

        var containerClient = AzureBlobClientFactory.CreateContainerClient(options);
        await containerClient.CreateIfNotExistsAsync();

        var gateway = new AzureBlobStorageGateway(containerClient);
        var storage = new AzureBlobFileStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(options));

        var key = StorageKey.Create(Guid.NewGuid(), ".txt", PersistenceTestConstants.BaseTimestamp);
        await using (var upload = new MemoryStream("azurite-integration"u8.ToArray()))
        {
            var url = await storage.UploadAsync(upload, "text/plain", key.Value);
            url.Should().Contain(key.Value);
        }

        (await storage.ExistsAsync(key.Value)).Should().BeTrue();

        await using (var download = await storage.OpenReadAsync(key.Value))
        {
            download.Should().NotBeNull();
            using var reader = new StreamReader(download!);
            (await reader.ReadToEndAsync()).Should().Be("azurite-integration");
        }

        await storage.DeleteAsync(key.Value);
        (await storage.ExistsAsync(key.Value)).Should().BeFalse();

        await containerClient.DeleteIfExistsAsync();
    }

    private static async Task<bool> IsAzuriteAvailableAsync()
    {
        try
        {
            var client = AzureBlobClientFactory.CreateContainerClient(new MediaStorageOptions
            {
                ContainerName = "probe",
                ConnectionString = _azuriteConnectionString,
            });

            await client.CreateIfNotExistsAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
