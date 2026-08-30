namespace ContentForge.Application.Abstractions;

/// <summary>
/// Supplies request-scoped audit context such as IP address and correlation identifiers.
/// </summary>
public interface IAuditRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }

    string? CorrelationId { get; }
}
