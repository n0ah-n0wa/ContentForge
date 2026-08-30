namespace ContentForge.Api.Infrastructure;

/// <summary>
/// Ensures every request has a correlation identifier for logging and error responses.
/// </summary>
internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger,
    IHostEnvironment environment)
{
    internal const string HeaderName = "X-Correlation-ID";
    internal const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["service"] = ObservabilityConstants.ServiceName,
            ["environment"] = environment.EnvironmentName,
            ["traceId"] = context.TraceIdentifier,
            ["correlationId"] = correlationId,
        });

        await next(context).ConfigureAwait(false);
    }
}
