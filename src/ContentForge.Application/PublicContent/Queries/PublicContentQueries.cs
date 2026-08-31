namespace ContentForge.Application.PublicContent.Queries;

using ContentForge.Application.Abstractions.Caching;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.Mapping;
using ContentForge.Application.PublicContent.Caching;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

public sealed record ListPublicContentQuery(PublicContentListCriteria Criteria);

public sealed class ListPublicContentQueryHandler
{
    private static readonly IReadOnlySet<string> _emptyFilterSet = new HashSet<string>(StringComparer.Ordinal);

    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IPublicContentCache _cache;

    public ListPublicContentQueryHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IPublicContentCache cache)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _cache = cache;
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

        var listCacheKey = PublicContentCacheListKeyBuilder.Build(query.Criteria);
        var cachedPage = await _cache.TryGetListAsync(
            query.Criteria.ContentTypeSlug,
            listCacheKey,
            cancellationToken).ConfigureAwait(false);

        if (cachedPage is not null)
        {
            return cachedPage;
        }

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
            Status: null,
            IncludeDeleted: false,
            Search: query.Criteria.Search,
            PublishedFrom: query.Criteria.PublishedFrom,
            PublishedTo: query.Criteria.PublishedTo,
            ExactSlug: exactSlug,
            PublishedRepresentationOnly: true);

        var result = await _contentEntryRepository.ListAsync(listCriteria, cancellationToken);
        var items = result.Items
            .Select(entry => ContentEntryMapper.ToPublicDto(contentType.Slug.Value, entry, contentType))
            .ToList();

        var page = new PaginatedResult<PublicContentDto>(items, result.Page, result.PageSize, result.TotalItems);
        await _cache.SetListAsync(
            contentType.Slug.Value,
            listCacheKey,
            page,
            cancellationToken).ConfigureAwait(false);

        return page;
    }
}

public sealed record GetPublicContentBySlugQuery(string ContentTypeSlug, string Slug);

public sealed class GetPublicContentBySlugQueryHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IPublicContentCache _cache;

    public GetPublicContentBySlugQueryHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IPublicContentCache cache)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _cache = cache;
    }

    public async Task<PublicContentDto> HandleAsync(GetPublicContentBySlugQuery query, CancellationToken cancellationToken)
    {
        var normalizedSlug = ApplicationGuard.CreateSlug(query.Slug).Value;
        var cachedEntry = await _cache.TryGetEntryAsync(
            query.ContentTypeSlug,
            normalizedSlug,
            cancellationToken).ConfigureAwait(false);

        if (cachedEntry is not null)
        {
            return cachedEntry;
        }

        var contentType = await PublicContentVisibility.RequireActiveContentTypeAsync(
            _contentTypeRepository,
            query.ContentTypeSlug,
            cancellationToken);

        var entry = await _contentEntryRepository.GetPublishedBySlugAsync(
                contentType.Id,
                ApplicationGuard.CreateSlug(query.Slug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", query.Slug);

        if (!PublicContentVisibility.IsPubliclyVisible(entry))
        {
            throw new NotFoundApplicationException("ContentEntry", query.Slug);
        }

        var dto = ContentEntryMapper.ToPublicDto(contentType.Slug.Value, entry, contentType);
        await _cache.SetEntryAsync(contentType.Slug.Value, normalizedSlug, dto, cancellationToken).ConfigureAwait(false);
        return dto;
    }
}

internal static class PublicContentVisibility
{
    internal static bool IsPubliclyVisible(ContentEntry entry) =>
        !entry.IsDeleted && entry.HasPublishedRepresentation;

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
