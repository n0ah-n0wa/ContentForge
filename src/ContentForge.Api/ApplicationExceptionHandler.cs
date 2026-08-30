namespace ContentForge.Api;

using System.Text.Json;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Api.Infrastructure;
using ContentForge.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return false;
        }

        if (exception is ApplicationException applicationException)
        {
            LogApplicationException(httpContext, applicationException);
            await WriteApplicationExceptionAsync(httpContext, applicationException, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        LogUnhandledException(httpContext, exception);
        var problem = ProblemDetailsFactory.Create(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred.",
            ApiConstants.ErrorTypes.Application);
        await ProblemDetailsFactory.WriteAsync(httpContext, problem, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private void LogApplicationException(HttpContext httpContext, ApplicationException exception)
    {
        var statusCode = ResolveStatusCode(exception);
        var level = statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : LogLevel.Warning;

        using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["service"] = ObservabilityConstants.ServiceName,
            ["traceId"] = httpContext.TraceIdentifier,
            ["correlationId"] = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId)
                ? correlationId
                : null,
            ["userId"] = httpContext.User.FindFirst("sub")?.Value,
            ["httpMethod"] = httpContext.Request.Method,
            ["httpPath"] = httpContext.Request.Path.Value,
            ["exceptionType"] = exception.GetType().Name,
            ["exceptionMessage"] = SensitiveTelemetryRedactor.Redact(exception.Message),
        });

        ExceptionTelemetryLogger.ApplicationException(
            logger,
            level,
            exception,
            exception.GetType().Name,
            statusCode);
    }

    private void LogUnhandledException(HttpContext httpContext, Exception exception)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["service"] = ObservabilityConstants.ServiceName,
            ["traceId"] = httpContext.TraceIdentifier,
            ["correlationId"] = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId)
                ? correlationId
                : null,
            ["userId"] = httpContext.User.FindFirst("sub")?.Value,
            ["httpMethod"] = httpContext.Request.Method,
            ["httpPath"] = httpContext.Request.Path.Value,
            ["exceptionType"] = exception.GetType().Name,
            ["exceptionMessage"] = SensitiveTelemetryRedactor.Redact(exception.Message),
        });

        ExceptionTelemetryLogger.UnhandledException(logger, exception, exception.GetType().Name);
    }

    private static int ResolveStatusCode(ApplicationException exception) =>
        exception switch
        {
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            UnauthorizedApplicationException => StatusCodes.Status401Unauthorized,
            ForbiddenApplicationException => StatusCodes.Status403Forbidden,
            NotFoundApplicationException => StatusCodes.Status404NotFound,
            UnsupportedQueryParameterException => StatusCodes.Status400BadRequest,
            ConcurrencyConflictApplicationException => StatusCodes.Status409Conflict,
            ApplicationValidationException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest,
        };

    private static async Task WriteApplicationExceptionAsync(
        HttpContext httpContext,
        ApplicationException applicationException,
        CancellationToken cancellationToken)
    {
        if (applicationException is ConcurrencyConflictApplicationException concurrencyException)
        {
            var conflictProblem = ProblemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                concurrencyException.Message,
                ApiConstants.ErrorTypes.Conflict);
            conflictProblem.Extensions["expectedVersion"] = concurrencyException.Conflict.ExpectedVersion;
            conflictProblem.Extensions["actualVersion"] = concurrencyException.Conflict.ActualVersion;
            conflictProblem.Extensions["updatedAt"] = concurrencyException.Conflict.UpdatedAt.ToString("O");
            await ProblemDetailsFactory.WriteAsync(httpContext, conflictProblem, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (applicationException is ApplicationValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            var payload = ProblemDetailsFactory.CreateValidationPayload(httpContext, validationException);
            await httpContext.Response.WriteAsJsonAsync(
                payload,
                _jsonOptions,
                "application/problem+json",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var (statusCode, title, detail, type) = applicationException switch
        {
            AuthenticationFailedException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication failed.",
                ApiConstants.ErrorTypes.Unauthorized),
            UnauthorizedApplicationException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                applicationException.Message,
                ApiConstants.ErrorTypes.Unauthorized),
            ForbiddenApplicationException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                applicationException.Message,
                ApiConstants.ErrorTypes.Forbidden),
            NotFoundApplicationException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                applicationException.Message,
                ApiConstants.ErrorTypes.NotFound),
            UnsupportedQueryParameterException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                applicationException.Message,
                ApiConstants.ErrorTypes.BadRequest),
            _ => (
                StatusCodes.Status400BadRequest,
                "Application Error",
                applicationException.Message,
                ApiConstants.ErrorTypes.Application),
        };

        var problem = ProblemDetailsFactory.Create(httpContext, statusCode, title, detail, type);
        await ProblemDetailsFactory.WriteAsync(httpContext, problem, cancellationToken).ConfigureAwait(false);
    }
}
