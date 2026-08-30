namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Media.Commands;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using ContentForge.UnitTests.Domain;
using FluentAssertions;
using NSubstitute;

public sealed class MediaHandlerTests
{
    [Fact]
    public async Task UploadMediaCommandHandler_UsesSystemStorageKeyAndPersistsMetadata()
    {
        var repository = Substitute.For<IMediaRepository>();
        var storage = Substitute.For<IFileStorage>();
        storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => $"/media-files/{call.ArgAt<string>(2)}");

        var handler = CreateUploadHandler(repository, storage);
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]);

        var result = await handler.HandleAsync(
            new UploadMediaCommand(stream, @"nested/cover.jpg", "image/jpeg", stream.Length, "Alt"),
            CancellationToken.None);

        result.OriginalFileName.Should().Be("cover.jpg");
        result.FileName.Should().MatchRegex("^[0-9a-f]{32}\\.jpg$");
        result.FileName.Should().NotContain("..");
        result.Url.Should().StartWith("/media-files/media/");
        result.AltText.Should().Be("Alt");

        await storage.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            "image/jpeg",
            Arg.Is<string>(key => key.StartsWith("media/", StringComparison.Ordinal) && !key.Contains("cover", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await repository.Received(1).AddAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadMediaCommandHandler_WhenPersistenceFails_DeletesUploadedBinary()
    {
        var repository = Substitute.For<IMediaRepository>();
        repository.When(repo => repo.AddAsync(Arg.Any<MediaAsset>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("database unavailable"));

        var storage = Substitute.For<IFileStorage>();
        storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("/media-files/media/2026/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg");

        var handler = CreateUploadHandler(repository, storage);
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]);

        var action = () => handler.HandleAsync(
            new UploadMediaCommand(stream, "cover.jpg", "image/jpeg", stream.Length),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        await storage.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadMediaCommandHandler_RejectsDisallowedMimeType()
    {
        var handler = CreateUploadHandler(Substitute.For<IMediaRepository>(), Substitute.For<IFileStorage>());
        await using var stream = new MemoryStream("payload"u8.ToArray());

        var action = () => handler.HandleAsync(
            new UploadMediaCommand(stream, "payload.exe", "application/octet-stream", stream.Length),
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }

    [Fact]
    public async Task UploadMediaCommandHandler_RejectsMimeSpoofedExecutable()
    {
        var handler = CreateUploadHandler(Substitute.For<IMediaRepository>(), Substitute.For<IFileStorage>());
        await using var stream = new MemoryStream([0x4D, 0x5A, 0x90, 0x00]);

        var action = () => handler.HandleAsync(
            new UploadMediaCommand(stream, "payload.jpg", "image/jpeg", stream.Length),
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>()
            .Where(exception => exception.Failures.Any(failure =>
                failure.Message.Contains("does not match", StringComparison.Ordinal)));
    }

    private static UploadMediaCommandHandler CreateUploadHandler(
        IMediaRepository repository,
        IFileStorage storage) =>
        new(
            repository,
            storage,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.EditorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new UploadMediaCommandValidator());
}
