namespace ContentForge.Application.Media.Queries;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Mapping;
using ContentForge.Application.Media.Models;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

public sealed record GetMediaQuery(Guid MediaId);

public sealed class GetMediaQueryHandler
{
    private readonly IMediaRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetMediaQueryHandler(IMediaRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<MediaAssetDto> HandleAsync(GetMediaQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaRead);

        var asset = await _repository.GetByIdAsync(MediaId.From(query.MediaId), cancellationToken)
            ?? throw new NotFoundApplicationException("MediaAsset", query.MediaId);

        if (asset.IsDeleted && !AuthorizationRules.IsAllowed(role, Permissions.MediaDelete))
        {
            throw new NotFoundApplicationException("MediaAsset", query.MediaId);
        }

        return MediaMapper.ToDto(asset);
    }
}

public sealed record ListMediaQuery(MediaListCriteria Criteria);

public sealed class ListMediaQueryHandler
{
    private readonly IMediaRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListMediaQueryHandler(IMediaRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<MediaAssetDto>> HandleAsync(ListMediaQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaRead);
        ApplicationGuard.EnsureCanIncludeDeleted(role, Permissions.MediaDelete, query.Criteria.IncludeDeleted);

        query.Criteria.Sort.EnsureAllowed(MediaListCriteria.AllowedSortFields, "media");
        ValidateFilters(query.Criteria);

        var result = await _repository.ListAsync(query.Criteria, cancellationToken);
        return new PaginatedResult<MediaAssetDto>(
            result.Items.Select(MediaMapper.ToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    private static void ValidateFilters(MediaListCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            supplied["search"] = criteria.Search;
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            supplied["contentType"] = criteria.ContentType;
        }

        if (criteria.IncludeDeleted)
        {
            supplied["includeDeleted"] = bool.TrueString;
        }

        FilterValidator.EnsureAllowed(supplied, MediaListCriteria.AllowedFilterFields, "media");
    }
}
