namespace ContentForge.Infrastructure.Services;

using ContentForge.Application.Abstractions;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Reads IP, user-agent, and correlation identifiers from the current HTTP request.
/// </summary>
internal sealed class HttpAuditRequestContext(IHttpContextAccessor httpContextAccessor) : IAuditRequestContext
{
    internal const string CorrelationIdItemKey = "CorrelationId";

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
        }
    }

    public string? CorrelationId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var correlationId)
                && correlationId is string correlationIdValue
                && !string.IsNullOrWhiteSpace(correlationIdValue))
            {
                return correlationIdValue;
            }

            return httpContext.TraceIdentifier;
        }
    }
}
