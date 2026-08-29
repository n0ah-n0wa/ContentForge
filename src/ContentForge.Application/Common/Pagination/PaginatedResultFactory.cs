namespace ContentForge.Application.Common.Pagination;

/// <summary>
/// Factory helpers for paginated results.
/// </summary>
public static class PaginatedResults
{
    public static PaginatedResult<T> Empty<T>(PaginationRequest pagination) =>
        new([], pagination.NormalizedPage, pagination.NormalizedPageSize, 0);
}
