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
        var page = pagination.NormalizedPage;
        var pageSize = pagination.NormalizedPageSize;

        var totalItems = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        if (totalItems == 0)
        {
            return new PaginatedResult<T>([], page, pageSize, 0);
        }

        var items = await query
            .Skip(pagination.Skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PaginatedResult<T>(items, page, pageSize, totalItems);
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
