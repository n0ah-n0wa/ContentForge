namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Common.Pagination;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;

/// <summary>
/// Read-only persistence port for audit log entries.
/// </summary>
public interface IAuditLogRepository
{
    Task<AuditLogEntry?> GetByIdAsync(AuditLogId id, CancellationToken cancellationToken = default);

    Task<PaginatedResult<AuditLogEntry>> ListAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken = default);
}
