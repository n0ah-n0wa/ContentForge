namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Users.Queries;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

/// <summary>
/// Persistence port for CMS user accounts.
/// </summary>
public interface IUserRepository
{
    Task<UserAccount?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<PaginatedResult<UserAccount>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken = default);

    Task AddAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDefinition>> ListRolesAsync(CancellationToken cancellationToken = default);
}
