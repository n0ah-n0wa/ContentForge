namespace ContentForge.Infrastructure.Observability;

/// <summary>
/// Azure Application Insights integration settings.
/// </summary>
public sealed class ApplicationInsightsOptions
{
    public const string SectionName = "ApplicationInsights";

    /// <summary>
    /// When true, exports telemetry when a connection string is configured.
    /// In Production and Staging, telemetry is also enabled automatically when a connection string is present.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Application Insights connection string. Prefer the APPLICATIONINSIGHTS_CONNECTION_STRING environment variable in Azure.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Trace sampling ratio between 0 and 1. Use lower values in high-traffic Production environments.
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;
}
