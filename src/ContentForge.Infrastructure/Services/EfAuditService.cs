namespace ContentForge.Infrastructure.Services;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Mapping;

internal sealed class EfAuditService(AppDbContext dbContext) : IAuditService
{
    public Task RecordAsync(
        AuditAction action,
        string entityType,
        string entityId,
        UserId? userId = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = AuditLogEntry.Create(
            action,
            entityType,
            entityId,
            userId,
            metadata,
            ipAddress,
            userAgent);

        dbContext.AuditLogs.Add(AuditLogMapper.ToEntity(entry));
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
