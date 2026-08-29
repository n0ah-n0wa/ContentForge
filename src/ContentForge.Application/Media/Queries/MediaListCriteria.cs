namespace ContentForge.Application.Media.Queries;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;

/// <summary>
/// Query criteria for listing media assets.
/// </summary>
public sealed record MediaListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    string? Search = null,
    string? ContentType = null,
    bool IncludeDeleted = false)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "fileName",
        "uploadedAt",
        "size",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "search",
        "contentType",
        "includeDeleted",
    };
}
