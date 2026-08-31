namespace ContentForge.Infrastructure.Services;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class EfContentSearchService(AppDbContext dbContext) : IContentSearchService
{
    public async Task<PaginatedResult<Guid>> SearchAsync(
        ContentSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContentEntries
            .AsNoTracking()
            .Where(entry => !entry.IsDeleted);

        if (criteria.ContentTypeId is { } contentTypeId)
        {
            query = query.Where(entry => entry.ContentTypeId == contentTypeId.Value);
        }

        if (criteria.Status is { } status)
        {
            var statusValue = status.ToString();
            query = query.Where(entry => entry.Status == statusValue);
        }

        if (criteria.AuthorId is { } authorId)
        {
            query = query.Where(entry => entry.CreatedBy == authorId.Value);
        }

        if (criteria.CreatedFrom is { } createdFrom)
        {
            query = query.Where(entry => entry.CreatedAt >= createdFrom);
        }

        if (criteria.CreatedTo is { } createdTo)
        {
            query = query.Where(entry => entry.CreatedAt <= createdTo);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var pattern = PortableSearch.CreateContainsPattern(criteria.Keyword);
            query = query.WhereContentSearchContains(dbContext, pattern);
        }

        query = ApplySort(query, criteria.Sort);

        return await query
            .Select(entry => entry.Id)
            .ToPaginatedResultAsync(criteria.Pagination, cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<ContentEntryEntity> ApplySort(
        IQueryable<ContentEntryEntity> query,
        SortRequest sort) =>
        sort.SortBy switch
        {
            "updatedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.UpdatedAt)
                : query.OrderBy(entry => entry.UpdatedAt),
            "publishedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.PublishedAt)
                : query.OrderBy(entry => entry.PublishedAt),
            "slug" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.Slug)
                : query.OrderBy(entry => entry.Slug),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.CreatedAt)
                : query.OrderBy(entry => entry.CreatedAt),
        };
}
