namespace ContentForge.Infrastructure.Observability;

using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// Redacts sensitive values from exported log telemetry.
/// </summary>
internal sealed class SensitiveTelemetryLogRecordProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (data.Attributes is null)
        {
            return;
        }

        var updatedAttributes = new List<KeyValuePair<string, object?>>(data.Attributes.Count);
        foreach (var attribute in data.Attributes)
        {
            if (attribute.Value is string stringValue && ShouldRedactAttribute(attribute.Key, stringValue))
            {
                updatedAttributes.Add(new KeyValuePair<string, object?>(attribute.Key, "[REDACTED]"));
                continue;
            }

            updatedAttributes.Add(attribute);
        }

        data.Attributes = updatedAttributes;
    }

    private static bool ShouldRedactAttribute(string key, string value) =>
        SensitiveTelemetryRedactor.IsSensitiveHeader(key)
        || key.Contains("password", StringComparison.OrdinalIgnoreCase)
        || key.Contains("token", StringComparison.OrdinalIgnoreCase)
        || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
        || value.Contains("eyJ", StringComparison.Ordinal);
}
