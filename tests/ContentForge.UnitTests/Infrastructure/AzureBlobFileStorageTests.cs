namespace ContentForge.UnitTests.Infrastructure;

using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.BlobStorage;
using ContentForge.UnitTests.Domain;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

public sealed class AzureBlobFileStorageTests
{
    [Fact]
    public async Task UploadAsync_DelegatesToGatewayAndReturnsConfiguredPublicUrl()
    {
        var gateway = Substitute.For<IBlobStorageGateway>();
        var key = StorageKey.Create(Guid.NewGuid(), ".jpg", DomainTestData.Timestamp);
        await using var content = new MemoryStream("azure-bytes"u8.ToArray());

        var storage = new AzureBlobFileStorage(
            gateway,
            Options.Create(new MediaStorageOptions { PublicBaseUrl = "https://cdn.example.com/media" }));

        var url = await storage.UploadAsync(content, "image/jpeg", key.Value);

        url.Should().Be($"https://cdn.example.com/media/{key.Value}");
        await gateway.Received(1).UploadAsync(key.Value, content, "image/jpeg", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenReadAsync_DelegatesToGateway()
    {
        var gateway = Substitute.For<IBlobStorageGateway>();
        var key = StorageKey.Create(Guid.NewGuid(), ".txt", DomainTestData.Timestamp);
        await using var expected = new MemoryStream("payload"u8.ToArray());
        gateway.OpenReadAsync(key.Value, Arg.Any<CancellationToken>()).Returns(expected);

        var storage = new AzureBlobFileStorage(gateway, Options.Create(new MediaStorageOptions()));

        var stream = await storage.OpenReadAsync(key.Value);

        stream.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToGateway()
    {
        var gateway = Substitute.For<IBlobStorageGateway>();
        var key = StorageKey.Create(Guid.NewGuid(), ".txt", DomainTestData.Timestamp);
        var storage = new AzureBlobFileStorage(gateway, Options.Create(new MediaStorageOptions()));

        await storage.DeleteAsync(key.Value);

        await gateway.Received(1).DeleteAsync(key.Value, Arg.Any<CancellationToken>());
    }
}
