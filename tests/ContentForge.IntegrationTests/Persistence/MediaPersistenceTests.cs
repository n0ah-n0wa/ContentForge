namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class MediaPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task MediaAsset_PersistsMetadataOnlyAndBinaryInObjectStorage()
    {
        await using var scope = CreatePersistenceScope();

        var mediaId = MediaId.New();
        var storageKey = StorageKey.Create(mediaId.Value, ".txt", PersistenceTestConstants.BaseTimestamp);
        await using var payload = new MemoryStream("binary-outside-sql"u8.ToArray());
        var url = await scope.FileStorage.UploadAsync(payload, "text/plain", storageKey.Value);

        var asset = MediaAsset.Create(
            mediaId,
            MediaUploadRules.CreateStoredFileName(mediaId.Value, ".txt"),
            "notes.txt",
            "text/plain",
            payload.Length,
            storageKey,
            PersistenceTestConstants.ActorId,
            url,
            uploadedAt: PersistenceTestConstants.BaseTimestamp);

        await scope.MediaAssets.AddAsync(asset);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.MediaAssets.GetByIdAsync(mediaId);
        loaded.Should().NotBeNull();
        loaded!.StorageKey.Value.Should().Be(storageKey.Value);
        loaded.Url.Should().Be(url);

        var entity = await scope.DbContext.MediaAssets.AsNoTracking().SingleAsync(media => media.Id == mediaId.Value);
        entity.StorageKey.Should().Be(storageKey.Value);
        typeof(MediaAssetEntity).GetProperties().Should().NotContain(property =>
            property.PropertyType == typeof(byte[]) || property.PropertyType == typeof(ReadOnlyMemory<byte>));

        (await scope.FileStorage.ExistsAsync(storageKey.Value)).Should().BeTrue();
        await using var stored = await scope.FileStorage.OpenReadAsync(storageKey.Value);
        using var reader = new StreamReader(stored!);
        (await reader.ReadToEndAsync()).Should().Be("binary-outside-sql");
    }

    [Fact]
    public void MediaAssetEntity_DoesNotDefineBinaryPayloadColumns()
    {
        typeof(MediaAssetEntity).GetProperties()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo(
            [
                "Id",
                "FileName",
                "OriginalFileName",
                "ContentType",
                "Size",
                "StorageKey",
                "Url",
                "Width",
                "Height",
                "AltText",
                "Title",
                "Description",
                "UploadedBy",
                "UploadedAt",
                "IsDeleted",
            ]);
    }
}
