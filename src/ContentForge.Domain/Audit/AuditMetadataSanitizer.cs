namespace ContentForge.Domain.Audit;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Prevents sensitive values from being persisted in audit metadata.
/// </summary>
public static partial class AuditMetadataSanitizer
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
    };

    public static string? Sanitize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        var trimmed = metadata.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                var node = JsonNode.Parse(trimmed);
                if (node is not null)
                {
                    SanitizeNode(node);
                    return node.ToJsonString();
                }
            }
            catch (JsonException)
            {
                return RedactInlineSecrets(trimmed);
            }
        }

        return RedactInlineSecrets(trimmed);
    }

    public static string Build(params (string Key, string? Value)[] pairs)
    {
        var sanitized = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                continue;
            }

            sanitized[key] = _sensitiveKeys.Contains(key) ? "[REDACTED]" : value;
        }

        return JsonSerializer.Serialize(sanitized);
    }

    private static void SanitizeNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (_sensitiveKeys.Contains(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                    continue;
                }

                if (property.Value is not null)
                {
                    SanitizeNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    SanitizeNode(item);
                }
            }
        }
    }

    private static string RedactInlineSecrets(string metadata) =>
        SensitiveAssignmentRegex().Replace(metadata, "$1=[REDACTED]");

    [GeneratedRegex(
        @"(?i)\b(password|passwordHash|newPassword|currentPassword|refreshToken|accessToken|token|secret|authorization|apiKey|clientSecret)\b\s*=\s*[^,\s}]+")]
    private static partial Regex SensitiveAssignmentRegex();
}
