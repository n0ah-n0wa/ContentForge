namespace ContentForge.Api.Infrastructure;

/// <summary>
/// Structured log events for HTTP request metadata.
/// </summary>
internal static partial class RequestLoggingLogger
{
    [LoggerMessage(
        EventId = 2001,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms")]
    public static partial void RequestCompleted(
        ILogger logger,
        LogLevel level,
        string method,
        string path,
        int statusCode,
        long elapsedMs);
}
