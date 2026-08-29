namespace ContentForge.Application.Abstractions;

/// <summary>
/// Unit-of-work boundary for transactional persistence operations.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
