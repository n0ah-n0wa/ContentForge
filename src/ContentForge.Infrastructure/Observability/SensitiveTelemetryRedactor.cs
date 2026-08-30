namespace ContentForge.Infrastructure.Observability;

using System.Text.RegularExpressions;

/// <summary>
/// Redacts sensitive values from log messages, exceptions, and exported telemetry.
/// </summary>
public static partial class SensitiveTelemetryRedactor
{
    private static readonly HashSet<string> _sensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "newPassword",
        "currentPassword",
        "refreshToken",
        "accessToken",
        "token",
        "secret",
        "authorization",
        "apiKey",
        "clientSecret",
        "signingKey",
        "connectionString",
    };

    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var redacted = BearerTokenRegex().Replace(value, "Bearer [REDACTED]");
        redacted = JwtRegex().Replace(redacted, "[REDACTED_JWT]");
        redacted = SensitiveAssignmentRegex().Replace(redacted, "$1=[REDACTED]");
        return redacted;
    }

    public static bool IsSensitiveHeader(string headerName) =>
        _sensitiveKeys.Contains(headerName);

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(
        @"(?i)\b(password|passwordHash|newPassword|currentPassword|refreshToken|accessToken|token|secret|authorization|apiKey|clientSecret|signingKey|connectionString)\b\s*=\s*[^,\s}]+")]
    private static partial Regex SensitiveAssignmentRegex();
}
