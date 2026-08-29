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

public sealed record ListPublicContentQuery(PublicContentListCriteria Criteria);

public sealed class ListPublicContentQueryHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;

    public ListPublicContentQueryHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
    }

    public async Task<PaginatedResult<PublicContentDto>> HandleAsync(ListPublicContentQuery query, CancellationToken cancellationToken)
    {
        query.Criteria.Sort.EnsureAllowed(PublicContentListCriteria.AllowedSortFields, "public content");
        FilterValidator.EnsureAllowed(query.Criteria.Filters, PublicContentListCriteria.AllowedFilterFields, "public content");

        var contentType = await _contentTypeRepository.GetBySlugAsync(
                ApplicationGuard.CreateSlug(query.Criteria.ContentTypeSlug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", query.Criteria.ContentTypeSlug);

        var listCriteria = new ContentEntryListCriteria(
            query.Criteria.Pagination,
            query.Criteria.Sort,
            contentType.Id,
            ContentStatus.Published,
            IncludeDeleted: false);

        var result = await _contentEntryRepository.ListAsync(listCriteria, cancellationToken);
        var items = result.Items
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
        var contentType = await _contentTypeRepository.GetBySlugAsync(
                ApplicationGuard.CreateSlug(query.ContentTypeSlug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", query.ContentTypeSlug);

        var entry = await _contentEntryRepository.GetBySlugAsync(
                contentType.Id,
                ApplicationGuard.CreateSlug(query.Slug),
                cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", query.Slug);

        if (!entry.HasPublishedRepresentation || entry.Status != ContentStatus.Published)
        {
            throw new NotFoundApplicationException("ContentEntry", query.Slug);
        }

        return ContentEntryMapper.ToPublicDto(contentType.Slug.Value, entry);
    }
}
