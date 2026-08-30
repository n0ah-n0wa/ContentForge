namespace ContentForge.Application.PublicContent.Queries;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

public sealed record ListPublicContentQuery(PublicContentListCriteria Criteria);

public sealed class ListPublicContentQueryHandler
{
    private static readonly IReadOnlySet<string> _emptyFilterSet = new HashSet<string>(StringComparer.Ordinal);

    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;

    public ListPublicContentQueryHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
    }

    public async Task<PaginatedResult<PublicContentDto>> HandleAsync(
        ListPublicContentQuery query,
        CancellationToken cancellationToken)
    {
        query.Criteria.Sort.EnsureAllowed(PublicContentListCriteria.AllowedSortFields, "public content");
        FilterValidator.EnsureAllowed(
            query.Criteria.UnsupportedFilters ?? new Dictionary<string, string?>(StringComparer.Ordinal),
            _emptyFilterSet,
            "public content");

        var contentType = await PublicContentVisibility.RequireActiveContentTypeAsync(
            _contentTypeRepository,
            query.Criteria.ContentTypeSlug,
            cancellationToken);

        var exactSlug = string.IsNullOrWhiteSpace(query.Criteria.Slug)
            ? null
            : ApplicationGuard.CreateSlug(query.Criteria.Slug).Value;

        var listCriteria = new ContentEntryListCriteria(
            query.Criteria.Pagination,
            query.Criteria.Sort,
            contentType.Id,
            ContentStatus.Published,
            IncludeDeleted: false,
            Search: query.Criteria.Search,
            PublishedFrom: query.Criteria.PublishedFrom,
            PublishedTo: query.Criteria.PublishedTo,
            ExactSlug: exactSlug);

        var result = await _contentEntryRepository.ListAsync(listCriteria, cancellationToken);
        var items = result.Items
            .Where(PublicContentVisibility.IsPubliclyVisible)
            .Select(entry => ContentEntryMapper.ToPublicDto(contentType.Slug.Value, entry))
            .ToList();

        return new PaginatedResult<PublicContentDto>(items, result.Page, result.PageSize, result.TotalItems);
    }
}

public sealed record GetPublicContentBySlugQuery(string ContentTypeSlug, string Slug);

public sealed class GetPublicContentBySlugQueryHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;

    public GetPublicContentBySlugQueryHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
    }

    public async Task<PublicContentDto> HandleAsync(GetPublicContentBySlugQuery query, CancellationToken cancellationToken)
    {
        var contentType = await PublicContentVisibility.RequireActiveContentTypeAsync(
            _contentTypeRepository,
            query.ContentTypeSlug,
            cancellationToken);

        var entry = await _contentEntryRepository.GetBySlugAsync(
                contentType.Id,
                ApplicationGuard.CreateSlug(query.Slug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", query.Slug);

        if (!PublicContentVisibility.IsPubliclyVisible(entry))
        {
            throw new NotFoundApplicationException("ContentEntry", query.Slug);
        }

        return ContentEntryMapper.ToPublicDto(contentType.Slug.Value, entry);
    }
}

internal static class PublicContentVisibility
{
    internal static bool IsPubliclyVisible(ContentEntry entry) =>
        !entry.IsDeleted
        && entry.Status == ContentStatus.Published
        && entry.HasPublishedRepresentation;

    internal static async Task<ContentType> RequireActiveContentTypeAsync(
        IContentTypeRepository contentTypeRepository,
        string contentTypeSlug,
        CancellationToken cancellationToken)
    {
        var contentType = await contentTypeRepository.GetBySlugAsync(
                ApplicationGuard.CreateSlug(contentTypeSlug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", contentTypeSlug);

        if (!contentType.IsActive)
        {
            throw new NotFoundApplicationException("ContentType", contentTypeSlug);
        }

        return contentType;
    }
}
