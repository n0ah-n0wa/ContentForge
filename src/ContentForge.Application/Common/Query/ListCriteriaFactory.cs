namespace ContentForge.Application.Common.Query;

using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Application.Media.Queries;
using ContentForge.Application.Users.Queries;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;

/// <summary>
/// Builds application list criteria from primitive query parameter values.
/// </summary>
public static class ListCriteriaFactory
{
    public static ContentTypeListCriteria CreateContentTypeListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        bool? isActive,
        string? search) =>
        new(pagination, sort, isActive, search);

    public static ContentEntryListCriteria CreateContentEntryListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        Guid? contentTypeId,
        string? status,
        Guid? authorId,
        string? search,
        bool includeDeleted) =>
        new(
            pagination,
            sort,
            contentTypeId is null ? null : ContentTypeId.From(contentTypeId.Value),
            ParseEnum<ContentStatus>(status, "status"),
            authorId is null ? null : UserId.From(authorId.Value),
            search,
            includeDeleted);

    public static ContentSearchCriteria CreateContentSearchCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        string? keyword,
        Guid? contentTypeId,
        string? status,
        Guid? authorId,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo) =>
        new(
            pagination,
            sort,
            keyword,
            contentTypeId is null ? null : ContentTypeId.From(contentTypeId.Value),
            ParseEnum<ContentStatus>(status, "status"),
            authorId is null ? null : UserId.From(authorId.Value),
            createdFrom,
            createdTo);

    public static MediaListCriteria CreateMediaListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        string? search,
        string? contentType,
        bool includeDeleted) =>
        new(pagination, sort, search, contentType, includeDeleted);

    public static UserListCriteria CreateUserListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        bool? isActive,
        string? role,
        string? search) =>
        new(pagination, sort, isActive, ParseEnum<RoleName>(role, "role"), search);

    public static AuditLogListCriteria CreateAuditLogListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        Guid? userId,
        string? action,
        string? entityType,
        string? entityId,
        DateTimeOffset? from,
        DateTimeOffset? to) =>
        new(
            pagination,
            sort,
            userId is null ? null : UserId.From(userId.Value),
            ParseEnum<AuditAction>(action, "action"),
            entityType,
            entityId,
            from,
            to);

    public static PublicContentListCriteria CreatePublicContentListCriteria(
        PaginationRequest pagination,
        SortRequest sort,
        string contentTypeSlug,
        string? slug,
        string? search,
        DateTimeOffset? publishedFrom,
        DateTimeOffset? publishedTo,
        IReadOnlyDictionary<string, string?> unsupportedFilters) =>
        new(pagination, sort, contentTypeSlug, slug, search, publishedFrom, publishedTo, unsupportedFilters);

    private static TEnum? ParseEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new UnsupportedQueryParameterException(
                parameterName,
                $"Value '{value}' is not supported for '{parameterName}'.");
        }

        return parsed;
    }
}
