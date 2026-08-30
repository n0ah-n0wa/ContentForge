namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Audit;
using FluentAssertions;

public sealed class AuditMetadataSanitizerTests
{
    [Fact]
    public void Sanitize_RedactsPasswordFieldsInJson()
    {
        var sanitized = AuditMetadataSanitizer.Sanitize(
            """{"password":"Secret123!","email":"user@example.com"}""");

        sanitized.Should().Contain("[REDACTED]");
        sanitized.Should().Contain("user@example.com");
        sanitized.Should().NotContain("Secret123!");
    }

    [Fact]
    public void Sanitize_RedactsInlineSecretAssignments()
    {
        var sanitized = AuditMetadataSanitizer.Sanitize("role=Author,password=Secret123!");

        sanitized.Should().Contain("role=Author");
        sanitized.Should().Contain("password=[REDACTED]");
        sanitized.Should().NotContain("Secret123!");
    }

    [Fact]
    public void Build_SkipsNullValuesAndRedactsSensitiveKeys()
    {
        var metadata = AuditMetadataSanitizer.Build(
            ("role", "Editor"),
            ("password", "should-not-appear"),
            ("ignored", null));

        metadata.Should().Contain("Editor");
        metadata.Should().Contain("[REDACTED]");
        metadata.Should().NotContain("should-not-appear");
        metadata.Should().NotContain("ignored");
    }
}
