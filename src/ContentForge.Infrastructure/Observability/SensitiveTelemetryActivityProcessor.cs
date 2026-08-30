namespace ContentForge.Infrastructure.Observability;

using System.Diagnostics;
using ContentForge.Infrastructure.Services;
using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// Removes sensitive values from exported trace telemetry.
/// </summary>
internal sealed class SensitiveTelemetryActivityProcessor : BaseProcessor<Activity>
{
    private const string _requestHeaderTagPrefix = "http.request.header.";
    private static readonly HashSet<string> _sensitiveTagKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "http.request.header.authorization",
        "http.request.header.cookie",
        "http.request.header.x-api-key",
        "http.request.header.x-auth-token",
        "db.statement",
        "db.query.text",
    };

    public override void OnEnd(Activity data)
    {
        foreach (var tag in data.TagObjects.ToArray())
        {
            if (ShouldRedactTag(tag.Key, tag.Value))
            {
                data.SetTag(tag.Key, "[REDACTED]");
            }
        }

        if (!string.IsNullOrWhiteSpace(data.StatusDescription))
        {
            data.SetStatus(data.Status, SensitiveTelemetryRedactor.Redact(data.StatusDescription));
        }
    }

    private static bool ShouldRedactTag(string key, object? value)
    {
        if (_sensitiveTagKeys.Contains(key))
        {
            return true;
        }

        if (key.StartsWith(_requestHeaderTagPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var headerName = key[_requestHeaderTagPrefix.Length..];
            if (SensitiveTelemetryRedactor.IsSensitiveHeader(headerName))
            {
                return true;
            }
        }

        return value is string stringValue && ContainsSensitiveContent(stringValue);
    }

    private static bool ContainsSensitiveContent(string value) =>
        value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
        || value.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase)
        || value.Contains("password=", StringComparison.OrdinalIgnoreCase)
        || value.Contains("eyJ", StringComparison.Ordinal);
}
