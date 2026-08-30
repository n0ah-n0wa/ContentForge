namespace ContentForge.Api.Infrastructure;

using System.Diagnostics;

/// <summary>
/// Logs request metadata without sensitive headers or bodies.
/// </summary>
internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsHealthProbe(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            var userId = context.User.FindFirst("sub")?.Value;
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : statusCode >= StatusCodes.Status400BadRequest
                    ? LogLevel.Warning
                    : LogLevel.Information;

            using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["service"] = ObservabilityConstants.ServiceName,
                ["environment"] = environment.EnvironmentName,
                ["traceId"] = context.TraceIdentifier,
                ["correlationId"] = context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId)
                    ? correlationId
                    : null,
                ["userId"] = userId,
                ["httpMethod"] = method,
                ["httpPath"] = path,
                ["statusCode"] = statusCode,
                ["elapsedMs"] = stopwatch.ElapsedMilliseconds,
            });

            RequestLoggingLogger.RequestCompleted(
                logger,
                level,
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static bool IsHealthProbe(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
}
