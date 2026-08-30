namespace ContentForge.Application.Content.Queries;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Domain.Content;
using ContentForge.Domain.Common;

/// <summary>
/// Query criteria for listing content entries in the admin API.
/// </summary>
public sealed record ContentEntryListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    ContentTypeId? ContentTypeId = null,
    ContentStatus? Status = null,
    UserId? AuthorId = null,
    string? Search = null,
    bool IncludeDeleted = false,
    DateTimeOffset? PublishedFrom = null,
    DateTimeOffset? PublishedTo = null,
    string? ExactSlug = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "slug",
        "status",
        "createdAt",
        "updatedAt",
        "publishedAt",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "contentTypeId",
        "status",
        "authorId",
        "search",
        "includeDeleted",
    };
}

/// <summary>
/// Query criteria for administrative content search.
/// </summary>
public sealed record ContentSearchCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    string? Keyword = null,
    ContentTypeId? ContentTypeId = null,
    ContentStatus? Status = null,
    UserId? AuthorId = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "createdAt",
        "updatedAt",
        "publishedAt",
        "slug",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "keyword",
        "contentTypeId",
        "status",
        "authorId",
        "createdFrom",
        "createdTo",
    };
}

/// <summary>
/// Query criteria for listing published public content.
/// </summary>
public sealed record PublicContentListCriteria(
    PaginationRequest Pagination,
    SortRequest Sort,
    string ContentTypeSlug,
    string? Slug = null,
    string? Search = null,
    DateTimeOffset? PublishedFrom = null,
    DateTimeOffset? PublishedTo = null,
    IReadOnlyDictionary<string, string?>? UnsupportedFilters = null)
{
    public static IReadOnlySet<string> AllowedSortFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "publishedAt",
        "slug",
    };

    public static IReadOnlySet<string> AllowedFilterFields { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "slug",
        "search",
        "publishedFrom",
        "publishedTo",
    };
}
