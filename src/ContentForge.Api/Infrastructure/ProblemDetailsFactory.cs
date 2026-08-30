namespace ContentForge.Api.Infrastructure;

using System.Text.Json;
using ContentForge.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

internal static class ProblemDetailsFactory
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    internal static Task WriteAsync(HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return httpContext.Response.WriteAsJsonAsync(problem, _jsonOptions, "application/problem+json", cancellationToken);
    }
    internal static ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string? detail,
        string type)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path,
        };

        Enrich(httpContext, problem);
        return problem;
    }

    internal static Dictionary<string, object?> CreateValidationPayload(
        HttpContext httpContext,
        ApplicationValidationException exception)
    {
        var errors = exception.Failures
            .GroupBy(failure => failure.Field, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.Message).ToArray(),
                StringComparer.Ordinal);

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = ApiConstants.ErrorTypes.Validation,
            ["title"] = "Validation failed",
            ["status"] = StatusCodes.Status422UnprocessableEntity,
            ["detail"] = exception.Message,
            ["instance"] = httpContext.Request.Path.Value,
            ["errors"] = errors,
            ["traceId"] = httpContext.TraceIdentifier,
            ["correlationId"] = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId)
                ? correlationId
                : null,
        };
    }

    internal static ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]> errors,
        string? detail = "One or more fields are invalid.")
    {
        var problem = new ValidationProblemDetails(
            errors.ToDictionary(
                entry => entry.Key,
                entry => entry.Value,
                StringComparer.Ordinal))
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed",
            Detail = detail,
            Type = ApiConstants.ErrorTypes.Validation,
            Instance = httpContext.Request.Path,
        };

        Enrich(httpContext, problem);
        return problem;
    }

    private static void Enrich(HttpContext httpContext, ProblemDetails problem)
    {
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId)
            && correlationId is string correlationIdValue)
        {
            problem.Extensions["correlationId"] = correlationIdValue;
        }
    }
}
