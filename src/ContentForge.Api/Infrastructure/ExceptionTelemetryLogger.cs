namespace ContentForge.Api.Infrastructure;

/// <summary>
/// Structured log events for centralized exception telemetry.
/// </summary>
internal static partial class ExceptionTelemetryLogger
{
    [LoggerMessage(
        EventId = 1001,
        Message = "Application exception {ExceptionType} with status {StatusCode}")]
    public static partial void ApplicationException(
        ILogger logger,
        LogLevel level,
        Exception exception,
        string exceptionType,
        int statusCode);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Unhandled exception {ExceptionType}")]
    public static partial void UnhandledException(
        ILogger logger,
        Exception exception,
        string exceptionType);
}
