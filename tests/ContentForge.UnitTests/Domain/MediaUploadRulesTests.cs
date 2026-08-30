namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using FluentAssertions;

public sealed class MediaUploadRulesTests
{
    [Fact]
    public void EnsureAllowed_AcceptsAllowedMimeAndExtensionPair()
    {
        var action = () => MediaUploadRules.EnsureAllowed("photo.JPG", "image/jpeg; charset=binary", 128);

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureAllowed_RejectsMimeExtensionMismatch()
    {
        var action = () => MediaUploadRules.EnsureAllowed("photo.jpg", "image/png", 128);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EnsureAllowed_RejectsDisallowedExtension()
    {
        var action = () => MediaUploadRules.EnsureAllowed("payload.exe", "application/octet-stream", 128);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EnsureAllowed_RejectsOversizedFiles()
    {
        var action = () => MediaUploadRules.EnsureAllowed(
            "photo.jpg",
            "image/jpeg",
            MediaUploadRules.MaxFileSizeBytes + 1);

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain(MediaUploadRules.MaxFileSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void IsolateFileName_StripsDirectoriesWithoutUsingThemAsStoragePaths()
    {
        MediaUploadRules.IsolateFileName(@"C:\uploads\nested\cover.png").Should().Be("cover.png");
    }

    [Fact]
    public void IsolateFileName_RejectsTraversalSegments()
    {
        var action = () => MediaUploadRules.IsolateFileName("../secrets.jpg");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void IsolateFileName_RejectsNullBytesAndControlCharacters()
    {
        var action = () => MediaUploadRules.IsolateFileName("cover\0.jpg");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("control characters");
    }

    [Fact]
    public void IsolateFileName_RejectsAlternateDataStreamMarkers()
    {
        var action = () => MediaUploadRules.IsolateFileName("cover.jpg:secret");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("alternate-stream");
    }
}

public sealed class StorageKeyTests
{
    [Fact]
    public void Create_UsesSystemGeneratedPathIndependentOfUserFileName()
    {
        var identifier = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var key = StorageKey.Create(identifier, ".jpg", DomainTestData.Timestamp);

        key.Value.Should().Be($"media/{DomainTestData.Timestamp.UtcDateTime:yyyy}/{identifier:N}.jpg");
        key.Value.Should().NotContain("cover");
        key.Value.Should().NotContain("..");
    }

    [Fact]
    public void Constructor_RejectsPathTraversal()
    {
        var action = () => new StorageKey("media/2026/../../../etc/passwd.jpg");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Constructor_RejectsUserSuppliedAbsolutePaths()
    {
        var action = () => new StorageKey(@"C:\Windows\System32\drivers\etc\hosts");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_RejectsDisallowedExtension()
    {
        var action = () => StorageKey.Create(Guid.NewGuid(), ".exe", DomainTestData.Timestamp);

        action.Should().Throw<DomainValidationException>();
    }
}
