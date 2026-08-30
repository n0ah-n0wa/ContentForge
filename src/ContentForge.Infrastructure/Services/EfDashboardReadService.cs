namespace ContentForge.Infrastructure.Services;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Dashboard.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class EfDashboardReadService(AppDbContext dbContext) : IDashboardReadService
{
    public async Task<DashboardContentStatisticsDto> GetContentStatisticsAsync(
        UserId? authorId,
        CancellationToken cancellationToken = default)
    {
        var query = BaseContentQuery(authorId);

        var grouped = await query
            .GroupBy(entry => entry.Status)
            .Select(group => new { Status = group.Key, Count = group.LongCount() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var counts = grouped.ToDictionary(item => item.Status, item => item.Count, StringComparer.Ordinal);
        var total = grouped.Sum(item => item.Count);

        return new DashboardContentStatisticsDto(
            total,
            counts.GetValueOrDefault(nameof(ContentStatus.Draft)),
            counts.GetValueOrDefault(nameof(ContentStatus.InReview)),
            counts.GetValueOrDefault(nameof(ContentStatus.Published)),
            counts.GetValueOrDefault(nameof(ContentStatus.Archived)));
    }

    public async Task<IReadOnlyList<DashboardRecentContentItemDto>> GetRecentContentAsync(
        UserId? authorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var entries = await BaseContentQuery(authorId)
            .OrderByDescending(entry => entry.UpdatedAt)
            .Take(limit)
            .Select(entry => new DashboardRecentContentItemDto(
                entry.Id,
                entry.ContentTypeId,
                entry.ContentType.Slug,
                entry.ContentType.DisplayName,
                entry.Slug,
                Enum.Parse<ContentStatus>(entry.Status),
                entry.UpdatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries;
    }

    public async Task<IReadOnlyList<DashboardRecentActivityItemDto>> GetRecentActivityAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var logs = await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .Select(log => new DashboardRecentActivityItemDto(
                log.Id,
                log.Timestamp,
                Enum.Parse<Domain.Audit.AuditAction>(log.Action),
                log.EntityType,
                log.EntityId,
                log.UserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return logs;
    }

    private IQueryable<ContentEntryEntity> BaseContentQuery(UserId? authorId)
    {
        var query = dbContext.ContentEntries
            .AsNoTracking()
            .Where(entry => !entry.IsDeleted);

        if (authorId is not null)
        {
            query = query.Where(entry => entry.CreatedBy == authorId.Value.Value);
        }

        return query;
    }
}
