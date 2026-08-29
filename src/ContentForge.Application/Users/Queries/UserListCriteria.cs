namespace ContentForge.Application.Users.Queries;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Domain.Authorization;

/// <summary>
/// Query criteria for listing CMS users.
/// </summary>
public sealed record UserListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    bool? IsActive = null,
    RoleName? Role = null,
    string? Search = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "email",
        "displayName",
        "createdAt",
        "lastLoginAt",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "isActive",
        "role",
        "search",
    };
}
