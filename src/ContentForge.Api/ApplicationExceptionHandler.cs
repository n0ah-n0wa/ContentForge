namespace ContentForge.Api;

using ContentForge.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class ApplicationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApplicationException applicationException)
        {
            return false;
        }

        var (statusCode, title) = applicationException switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedApplicationException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenApplicationException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundApplicationException => (StatusCodes.Status404NotFound, "Not Found"),
            ApplicationValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            ConcurrencyConflictApplicationException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status400BadRequest, "Application Error"),
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = applicationException.Message,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
