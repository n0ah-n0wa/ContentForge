namespace ContentForge.Application.ContentTypes.Queries;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;

/// <summary>
/// Query criteria for listing content types.
/// </summary>
public sealed record ContentTypeListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    bool? IsActive = null,
    string? Search = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "name",
        "displayName",
        "createdAt",
        "updatedAt",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "isActive",
        "search",
    };
}
