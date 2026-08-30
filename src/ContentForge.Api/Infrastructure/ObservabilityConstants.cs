namespace ContentForge.Api.Infrastructure;

/// <summary>
/// Shared observability identifiers.
/// </summary>
internal static class ObservabilityConstants
{
    internal const string ServiceName = "ContentForge.Api";
    internal const string LiveHealthEndpoint = "/health/live";
    internal const string ReadyHealthEndpoint = "/health/ready";
}
