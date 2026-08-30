namespace ContentForge.UnitTests.Infrastructure;

using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.BlobStorage;
using FluentAssertions;

public sealed class AzureBlobClientFactoryTests
{
    [Fact]
    public void ValidateAzureOptions_WithConnectionString_DoesNotThrow()
    {
        var options = new MediaStorageOptions
        {
            ContainerName = "media",
            ConnectionString = "UseDevelopmentStorage=true",
        };

        var action = () => AzureBlobClientFactory.ValidateAzureOptions(options);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateAzureOptions_WithManagedIdentityAndAccountName_DoesNotThrow()
    {
        var options = new MediaStorageOptions
        {
            ContainerName = "media",
            StorageAccountName = "contentforgeprod",
            UseManagedIdentity = true,
        };

        var action = () => AzureBlobClientFactory.ValidateAzureOptions(options);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateAzureOptions_WithoutCredentials_Throws()
    {
        var options = new MediaStorageOptions
        {
            ContainerName = "media",
            UseManagedIdentity = false,
        };

        var action = () => AzureBlobClientFactory.ValidateAzureOptions(options);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*ManagedIdentity*");
    }

    [Fact]
    public void CreateContainerClient_WithAccountName_BuildsProductionServiceUri()
    {
        var options = new MediaStorageOptions
        {
            ContainerName = "media",
            StorageAccountName = "contentforgeprod",
            UseManagedIdentity = true,
        };

        var client = AzureBlobClientFactory.CreateContainerClient(options);

        client.Uri.Should().Be(new Uri("https://contentforgeprod.blob.core.windows.net/media"));
    }
}
