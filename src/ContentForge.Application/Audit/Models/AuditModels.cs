namespace ContentForge.Application.Audit.Models;

using ContentForge.Domain.Audit;

/// <summary>
/// Audit log entry returned by administrative queries.
/// </summary>
public sealed record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? UserId,
    AuditAction Action,
    string EntityType,
    string EntityId,
    string? Metadata,
    string? IpAddress,
    string? UserAgent);
