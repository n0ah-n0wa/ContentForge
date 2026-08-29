namespace ContentForge.Application.Common.Pagination;

/// <summary>
/// Standard paginated response envelope for collection queries.
/// </summary>
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems)
{
    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
