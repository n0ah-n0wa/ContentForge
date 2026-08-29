namespace ContentForge.Application.Users.Queries;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Mapping;
using ContentForge.Application.Users.Models;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

public sealed record GetUserQuery(Guid UserId);

public sealed class GetUserQueryHandler
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetUserQueryHandler(IUserRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserRead);

        var user = await _repository.GetByIdAsync(UserId.From(query.UserId), cancellationToken)
            ?? throw new NotFoundApplicationException("User", query.UserId);

        return UserMapper.ToDto(user);
    }
}

public sealed record ListUsersQuery(UserListCriteria Criteria);

public sealed class ListUsersQueryHandler
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListUsersQueryHandler(IUserRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<UserDto>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserRead);

        query.Criteria.Sort.EnsureAllowed(UserListCriteria.AllowedSortFields, "users");
        ValidateFilters(query.Criteria);

        var result = await _repository.ListAsync(query.Criteria, cancellationToken);
        return new PaginatedResult<UserDto>(
            result.Items.Select(UserMapper.ToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    private static void ValidateFilters(UserListCriteria criteria)
    {
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (criteria.IsActive is not null)
        {
            supplied["isActive"] = criteria.IsActive.Value.ToString();
        }

        if (criteria.Role is not null)
        {
            supplied["role"] = criteria.Role.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            supplied["search"] = criteria.Search;
        }

        FilterValidator.EnsureAllowed(supplied, UserListCriteria.AllowedFilterFields, "users");
    }
}

public sealed record ListRolesQuery;

public sealed class ListRolesQueryHandler
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ListRolesQueryHandler(IUserRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RoleDto>> HandleAsync(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserRead);

        var roles = await _repository.ListRolesAsync(cancellationToken);
        return roles
            .Select(definition => new RoleDto(definition.Name, definition.Permissions.Select(permission => permission.Value).ToList()))
            .ToList();
    }
}
