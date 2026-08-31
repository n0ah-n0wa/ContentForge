namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Content;
using FluentAssertions;

public sealed class RichTextSanitizerTests
{
    [Fact]
    public void Sanitize_RemovesScriptTagsAndEventHandlers()
    {
        const string malicious = "<p>Hello</p><img src=x onerror=\"alert(1)\"><script>alert(1)</script>";

        var sanitized = RichTextSanitizer.Sanitize(malicious);

        sanitized.Should().Contain("Hello");
        sanitized.Should().NotContain("<script");
        sanitized.Should().NotContain("onerror");
    }

    [Fact]
    public void Sanitize_BlocksJavascriptUrlsInLinks()
    {
        const string malicious = "<a href=\"javascript:alert(1)\">Click me</a>";

        var sanitized = RichTextSanitizer.Sanitize(malicious);

        sanitized.Should().NotContain("javascript:");
    }

    [Fact]
    public void Sanitize_PreservesSafeMarkup()
    {
        const string safe = "<p><strong>Title</strong></p><ul><li>Item</li></ul>";

        var sanitized = RichTextSanitizer.Sanitize(safe);

        sanitized.Should().Contain("<strong>Title</strong>");
        sanitized.Should().Contain("<li>Item</li>");
    }
}
