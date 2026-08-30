namespace ContentForge.Api;

using System.Text.Json;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApplicationExceptionHandler : IExceptionHandler
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
            await WriteApplicationExceptionAsync(httpContext, applicationException, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        var problem = ProblemDetailsFactory.Create(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred.",
            ApiConstants.ErrorTypes.Application);
        await ProblemDetailsFactory.WriteAsync(httpContext, problem, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static async Task WriteApplicationExceptionAsync(
        HttpContext httpContext,
        ApplicationException applicationException,
        CancellationToken cancellationToken)
    {
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
            ConcurrencyConflictApplicationException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                applicationException.Message,
                ApiConstants.ErrorTypes.Conflict),
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
