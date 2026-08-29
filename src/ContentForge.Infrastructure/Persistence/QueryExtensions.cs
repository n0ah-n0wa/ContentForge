namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using Microsoft.EntityFrameworkCore;

internal static class QueryExtensions
{
    internal static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var totalItems = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (totalItems == 0)
        {
            return new PaginatedResult<T>([], pagination.Page, pagination.PageSize, 0);
        }

        var skip = (pagination.Page - 1) * pagination.PageSize;
        var items = await query
            .Skip(skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<T>(items, pagination.Page, pagination.PageSize, totalItems);
    }

    internal static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        SortRequest sort,
        IReadOnlyDictionary<string, Func<IQueryable<T>, SortDirection, IQueryable<T>>> sortMap,
        string defaultSortField)
    {
        var sortField = sortMap.ContainsKey(sort.SortBy) ? sort.SortBy : defaultSortField;
        return sortMap[sortField](query, sort.Direction);
    }
}
