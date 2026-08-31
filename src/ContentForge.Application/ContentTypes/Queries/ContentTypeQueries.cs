namespace ContentForge.Application.ContentTypes.Queries;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

public sealed record GetContentTypeQuery(Guid ContentTypeId);

public sealed class GetContentTypeQueryHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetContentTypeQueryHandler(IContentTypeRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ContentTypeDto> HandleAsync(GetContentTypeQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeRead);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(query.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", query.ContentTypeId);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record ListContentTypesQuery(ContentTypeListCriteria Criteria);

public sealed class ListContentTypesQueryHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListContentTypesQueryHandler(IContentTypeRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ContentTypeDto>> HandleAsync(ListContentTypesQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeRead);

        query.Criteria.Sort.EnsureAllowed(ContentTypeListCriteria.AllowedSortFields, "content types");
        ValidateFilters(query.Criteria);

        var result = await _repository.ListAsync(query.Criteria, cancellationToken);
        return new PaginatedResult<ContentTypeDto>(
            result.Items.Select(ContentTypeMapper.ToListDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    private static void ValidateFilters(ContentTypeListCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (criteria.IsActive is not null)
        {
            supplied["isActive"] = criteria.IsActive.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            supplied["search"] = criteria.Search;
        }

        FilterValidator.EnsureAllowed(supplied, ContentTypeListCriteria.AllowedFilterFields, "content types");
    }
}
