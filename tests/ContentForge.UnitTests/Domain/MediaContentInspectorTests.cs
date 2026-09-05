namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using FluentAssertions;

public sealed class MediaContentInspectorTests
{
    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF, 0xD9];

    private static readonly byte[] _pngSignature =
        [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0];

    [Fact]
    public void EnsureMatchesDeclaredType_AcceptsValidJpegContent()
    {
        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(_jpegSignature, ".jpg");

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsExecutableContentWithJpegExtension()
    {
        var action = () => MediaContentInspector.EnsureMatchesDeclaredType([0x4D, 0x5A], ".jpg");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("does not match");
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsDoubleExtensionSpoof()
    {
        var action = () => MediaContentInspector.EnsureMatchesDeclaredType([0x4D, 0x5A, 0x90, 0x00], ".jpg");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsSvgWithScriptPayload()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(svg, ".svg");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("script");
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsSvgWithEventHandler()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"><circle cx=\"5\" cy=\"5\" r=\"4\"/></svg>"u8.ToArray();

        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(svg, ".svg");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("script");
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsSvgWithForeignObject()
    {
        var svg =
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><foreignObject><body xmlns=\"http://www.w3.org/1999/xhtml\">x</body></foreignObject></svg>"u8.ToArray();

        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(svg, ".svg");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void EnsureMatchesDeclaredType_AcceptsSafeSvg()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"5\" cy=\"5\" r=\"4\"/></svg>"u8.ToArray();

        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(svg, ".svg");

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureMatchesDeclaredType_RejectsTextWithNullBytes()
    {
        var action = () => MediaContentInspector.EnsureMatchesDeclaredType("before\0after"u8.ToArray(), ".txt");

        action.Should().Throw<DomainValidationException>()
            .Which.Message.Should().Contain("null bytes");
    }

    [Fact]
    public void EnsureMatchesDeclaredType_AcceptsValidPngContent()
    {
        var action = () => MediaContentInspector.EnsureMatchesDeclaredType(_pngSignature, ".png");

        action.Should().NotThrow();
    }
}

public sealed class MediaUploadStreamValidatorTests
{
    [Fact]
    public async Task BufferAndValidateAsync_RejectsStreamShorterThanDeclaredSize()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]);

        var action = () => MediaUploadStreamValidator.BufferAndValidateAsync(
            stream,
            "photo.jpg",
            "image/jpeg",
            declaredSize: 1024);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public async Task BufferAndValidateAsync_RejectsStreamLongerThanDeclaredSize()
    {
        await using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9, 0x00]);

        var action = () => MediaUploadStreamValidator.BufferAndValidateAsync(
            stream,
            "photo.jpg",
            "image/jpeg",
            declaredSize: 4);

        await action.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*does not match*");
    }

    [Fact]
    public async Task BufferAndValidateAsync_AcceptsMatchingJpegStream()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        await using var stream = new MemoryStream(bytes);

        var validated = await MediaUploadStreamValidator.BufferAndValidateAsync(
            stream,
            "photo.jpg",
            "image/jpeg",
            bytes.LongLength);

        validated.Length.Should().Be(bytes.Length);
        validated.ToArray().Should().Equal(bytes);
    }
}
