namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Common;
using Microsoft.EntityFrameworkCore;

internal sealed class EfUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(0, 0);
        }
    }
}
