namespace ContentForge.Application.Content.Queries;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentValidation;

public sealed record GetContentEntryQuery(Guid ContentEntryId);

public sealed class GetContentEntryQueryHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetContentEntryQueryHandler(IContentEntryRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ContentEntryDto> HandleAsync(GetContentEntryQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRead);

        var entry = ApplicationGuard.RequireVisibleContentEntry(
            await _repository.GetByIdAsync(ContentEntryId.From(query.ContentEntryId), cancellationToken),
            query.ContentEntryId);

        ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record ListContentEntriesQuery(ContentEntryListCriteria Criteria);

public sealed class ListContentEntriesQueryHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListContentEntriesQueryHandler(IContentEntryRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ContentEntryDto>> HandleAsync(ListContentEntriesQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRead);
        ApplicationGuard.EnsureCanIncludeDeleted(role, Permissions.ContentDelete, query.Criteria.IncludeDeleted);

        query.Criteria.Sort.EnsureAllowed(ContentEntryListCriteria.AllowedSortFields, "content entries");
        ValidateFilters(query.Criteria);

        var criteria = AuthorizationRules.CanModifyOwnContentOnly(role.Name)
            ? query.Criteria with { AuthorId = userId }
            : query.Criteria;

        var result = await _repository.ListAsync(criteria, cancellationToken);
        return new PaginatedResult<ContentEntryDto>(
            result.Items.Select(ContentEntryMapper.ToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    private static void ValidateFilters(ContentEntryListCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (criteria.ContentTypeId is not null)
        {
            supplied["contentTypeId"] = criteria.ContentTypeId.Value.Value.ToString();
        }

        if (criteria.Status is not null)
        {
            supplied["status"] = criteria.Status.Value.ToString();
        }

        if (criteria.AuthorId is not null)
        {
            supplied["authorId"] = criteria.AuthorId.Value.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            supplied["search"] = criteria.Search;
        }

        if (criteria.IncludeDeleted)
        {
            supplied["includeDeleted"] = bool.TrueString;
        }

        FilterValidator.EnsureAllowed(supplied, ContentEntryListCriteria.AllowedFilterFields, "content entries");
    }
}

public sealed record ListContentVersionsQuery(Guid ContentEntryId);

public sealed class ListContentVersionsQueryHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListContentVersionsQueryHandler(IContentEntryRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ContentVersionDto>> HandleAsync(ListContentVersionsQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentVersionRead);

        var entry = ApplicationGuard.RequireVisibleContentEntry(
            await _repository.GetByIdAsync(ContentEntryId.From(query.ContentEntryId), cancellationToken),
            query.ContentEntryId);

        ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);

        return entry.Versions
            .OrderBy(version => version.VersionNumber.Value)
            .Select(ContentEntryMapper.ToVersionDto)
            .ToList();
    }
}

public sealed record GetContentVersionQuery(Guid ContentEntryId, int VersionNumber);

public sealed class GetContentVersionQueryHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetContentVersionQueryHandler(IContentEntryRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ContentVersionDto> HandleAsync(GetContentVersionQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentVersionRead);

        var entry = ApplicationGuard.RequireVisibleContentEntry(
            await _repository.GetByIdAsync(ContentEntryId.From(query.ContentEntryId), cancellationToken),
            query.ContentEntryId);

        ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);

        var version = ApplicationGuard.TranslateDomainException(() => entry.GetVersion(VersionNumber.From(query.VersionNumber)))
            ?? throw new NotFoundApplicationException("ContentVersion", query.VersionNumber);

        return ContentEntryMapper.ToVersionDto(version);
    }
}

public sealed record CompareContentVersionsQuery(Guid ContentEntryId, int LeftVersionNumber, int RightVersionNumber);

public sealed class CompareContentVersionsQueryValidator : AbstractValidator<CompareContentVersionsQuery>
{
    public CompareContentVersionsQueryValidator()
    {
        RuleFor(query => query.ContentEntryId).NotEmpty();
        RuleFor(query => query.LeftVersionNumber).GreaterThan(0);
        RuleFor(query => query.RightVersionNumber).GreaterThan(0);
    }
}

public sealed class CompareContentVersionsQueryHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CompareContentVersionsQuery> _validator;

    public CompareContentVersionsQueryHandler(
        IContentEntryRepository repository,
        ICurrentUserService currentUser,
        IValidator<CompareContentVersionsQuery> validator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<ContentVersionComparisonDto> HandleAsync(CompareContentVersionsQuery query, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, query, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentVersionRead);

        var entry = ApplicationGuard.RequireVisibleContentEntry(
            await _repository.GetByIdAsync(ContentEntryId.From(query.ContentEntryId), cancellationToken),
            query.ContentEntryId);

        ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);

        var left = ApplicationGuard.TranslateDomainException(() => entry.GetVersion(VersionNumber.From(query.LeftVersionNumber)))
            ?? throw new NotFoundApplicationException("ContentVersion", query.LeftVersionNumber);
        var right = ApplicationGuard.TranslateDomainException(() => entry.GetVersion(VersionNumber.From(query.RightVersionNumber)))
            ?? throw new NotFoundApplicationException("ContentVersion", query.RightVersionNumber);

        var changes = ContentVersionComparer.Compare(left, right)
            .Select(change => new ContentFieldChangeDto(change.FieldName, change.OldValue, change.NewValue))
            .ToList();

        return new ContentVersionComparisonDto(query.LeftVersionNumber, query.RightVersionNumber, changes);
    }
}

public sealed record SearchContentQuery(ContentSearchCriteria Criteria);

public sealed class SearchContentQueryHandler
{
    private readonly IContentSearchService _searchService;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly ICurrentUserService _currentUser;

    public SearchContentQueryHandler(
        IContentSearchService searchService,
        IContentEntryRepository contentEntryRepository,
        ICurrentUserService currentUser)
    {
        _searchService = searchService;
        _contentEntryRepository = contentEntryRepository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ContentEntryDto>> HandleAsync(SearchContentQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRead);

        query.Criteria.Sort.EnsureAllowed(ContentSearchCriteria.AllowedSortFields, "content search");
        ValidateFilters(query.Criteria);

        var criteria = AuthorizationRules.CanModifyOwnContentOnly(role.Name)
            ? query.Criteria with { AuthorId = userId }
            : query.Criteria;

        var searchResult = await _searchService.SearchAsync(criteria, cancellationToken);
        var entries = new List<ContentEntryDto>();

        foreach (var entryId in searchResult.Items)
        {
            var entry = await _contentEntryRepository.GetByIdAsync(ContentEntryId.From(entryId), cancellationToken);
            if (entry is null)
            {
                continue;
            }

            ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);
            entries.Add(ContentEntryMapper.ToDto(entry));
        }

        return new PaginatedResult<ContentEntryDto>(
            entries,
            searchResult.Page,
            searchResult.PageSize,
            searchResult.TotalItems);
    }

    private static void ValidateFilters(ContentSearchCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            supplied["keyword"] = criteria.Keyword;
        }

        if (criteria.ContentTypeId is not null)
        {
            supplied["contentTypeId"] = criteria.ContentTypeId.Value.Value.ToString();
        }

        if (criteria.Status is not null)
        {
            supplied["status"] = criteria.Status.Value.ToString();
        }

        if (criteria.AuthorId is not null)
        {
            supplied["authorId"] = criteria.AuthorId.Value.Value.ToString();
        }

        if (criteria.CreatedFrom is not null)
        {
            supplied["createdFrom"] = criteria.CreatedFrom.Value.ToString("O");
        }

        if (criteria.CreatedTo is not null)
        {
            supplied["createdTo"] = criteria.CreatedTo.Value.ToString("O");
        }

        FilterValidator.EnsureAllowed(supplied, ContentSearchCriteria.AllowedFilterFields, "content search");
    }
}
