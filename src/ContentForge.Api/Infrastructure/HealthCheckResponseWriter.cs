namespace ContentForge.Api.Infrastructure;

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Writes minimal health-check responses without internal details or secrets.
/// </summary>
internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    internal static Task WriteMinimalAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = report.Status.ToString(),
        };

        if (report.Entries.Count > 0)
        {
            payload["checks"] = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status.ToString(),
                StringComparer.Ordinal);
        }

        return context.Response.WriteAsJsonAsync(payload, _jsonOptions);
    }
}
