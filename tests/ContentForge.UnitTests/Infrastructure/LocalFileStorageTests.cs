namespace ContentForge.UnitTests.Infrastructure;

using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Storage;
using ContentForge.UnitTests.Domain;
using FluentAssertions;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "contentforge-local-storage-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        Directory.CreateDirectory(_root);
        _storage = new LocalFileStorage(_root, "/media-files");
    }

    [Fact]
    public async Task UploadAsync_StoresBytesUnderSystemKeyNotOriginalFileName()
    {
        var key = StorageKey.Create(Guid.NewGuid(), ".txt", DomainTestData.Timestamp);
        await using var content = new MemoryStream("hello-media"u8.ToArray());

        var url = await _storage.UploadAsync(content, "text/plain", key.Value);

        url.Should().Be($"/media-files/{key.Value}");
        (await _storage.ExistsAsync(key.Value)).Should().BeTrue();

        var expectedPath = Path.Combine(_root, key.Value.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(expectedPath).Should().BeTrue();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
            .Should()
            .NotContain(path => path.Contains("user-chosen-name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UploadAsync_RejectsUnsafeStorageKeys()
    {
        await using var content = new MemoryStream("payload"u8.ToArray());

        var action = async () => await _storage.UploadAsync(
            content,
            "text/plain",
            "../outside.txt");

        await action.Should().ThrowAsync<DomainValidationException>();
        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_RemovesStoredBinary()
    {
        var key = StorageKey.Create(Guid.NewGuid(), ".txt", DomainTestData.Timestamp);
        await using var content = new MemoryStream("delete-me"u8.ToArray());
        await _storage.UploadAsync(content, "text/plain", key.Value);

        await _storage.DeleteAsync(key.Value);

        (await _storage.ExistsAsync(key.Value)).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
