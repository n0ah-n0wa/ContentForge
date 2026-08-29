namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Users.Queries;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfUserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<UserAccount?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadUserQuery()
            .SingleOrDefaultAsync(user => user.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : UserAccountMapper.ToDomain(entity);
    }

    public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var entity = await LoadUserQuery()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : UserAccountMapper.ToDomain(entity);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task<PaginatedResult<UserAccount>> ListAsync(
        UserListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = LoadUserQuery();

        if (criteria.IsActive is { } isActive)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        if (criteria.Role is { } role)
        {
            var roleName = role.ToString();
            query = query.Where(user => user.UserRoles.Any(userRole => userRole.Role.Name == roleName));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{PortableSearch.NormalizeTerm(criteria.Search)}%";
            query = query.WhereUserContains(dbContext, pattern);
        }

        query = ApplySort(query, criteria.Sort);

        var page = await query.ToPaginatedResultAsync(criteria.Pagination, cancellationToken).ConfigureAwait(false);
        return new PaginatedResult<UserAccount>(
            page.Items.Select(UserAccountMapper.ToDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task AddAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(UserAccountMapper.ToEntity(user), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .Include(account => account.UserRoles)
            .SingleOrDefaultAsync(account => account.Id == user.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{user.Id}' was not found.");

        UserAccountMapper.UpdateEntity(entity, user);
    }

    public async Task<IReadOnlyList<RoleDefinition>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles
            .AsNoTracking()
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles.Select(RoleDefinitionMapper.ToDomain).ToList();
    }

    private IQueryable<UserEntity> LoadUserQuery() =>
        dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role);

    private static IQueryable<UserEntity> ApplySort(IQueryable<UserEntity> query, SortRequest sort) =>
        sort.SortBy switch
        {
            "displayName" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(user => user.DisplayName)
                : query.OrderBy(user => user.DisplayName),
            "createdAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(user => user.CreatedAt)
                : query.OrderBy(user => user.CreatedAt),
            "lastLoginAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(user => user.LastLoginAt)
                : query.OrderBy(user => user.LastLoginAt),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(user => user.Email)
                : query.OrderBy(user => user.Email),
        };
}
