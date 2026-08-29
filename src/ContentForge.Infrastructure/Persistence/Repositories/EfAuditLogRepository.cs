namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Audit.Queries;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfAuditLogRepository(AppDbContext dbContext) : IAuditLogRepository
{
    public async Task<AuditLogEntry?> GetByIdAsync(AuditLogId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AuditLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(log => log.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : AuditLogMapper.ToDomain(entity);
    }

    public async Task<PaginatedResult<AuditLogEntry>> ListAsync(
        AuditLogListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsNoTracking();

        if (criteria.UserId is { } userId)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (criteria.Action is { } action)
        {
            var actionValue = action.ToString();
            query = query.Where(log => log.Action == actionValue);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType))
        {
            query = query.Where(log => log.EntityType == criteria.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityId))
        {
            query = query.Where(log => log.EntityId == criteria.EntityId);
        }

        if (criteria.From is { } from)
        {
            query = query.Where(log => log.Timestamp >= from);
        }

        if (criteria.To is { } to)
        {
            query = query.Where(log => log.Timestamp <= to);
        }

        query = ApplySort(query, criteria.Sort);

        var page = await query.ToPaginatedResultAsync(criteria.Pagination, cancellationToken).ConfigureAwait(false);
        return new PaginatedResult<AuditLogEntry>(
            page.Items.Select(AuditLogMapper.ToDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    private static IQueryable<AuditLogEntity> ApplySort(IQueryable<AuditLogEntity> query, SortRequest sort) =>
        sort.SortBy switch
        {
            "action" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(log => log.Action)
                : query.OrderBy(log => log.Action),
            "entityType" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(log => log.EntityType)
                : query.OrderBy(log => log.EntityType),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(log => log.Timestamp)
                : query.OrderBy(log => log.Timestamp),
        };
}
