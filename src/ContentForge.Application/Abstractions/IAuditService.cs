namespace ContentForge.Application.Abstractions;

using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;

/// <summary>
/// Records immutable audit events after successful operations.
/// </summary>
public interface IAuditService
{
    Task RecordAsync(
        AuditAction action,
        string entityType,
        string entityId,
        UserId? userId = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
