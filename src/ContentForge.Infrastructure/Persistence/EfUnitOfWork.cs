namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class EfUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw CreateConcurrencyConflict(exception);
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static ConcurrencyConflictException CreateConcurrencyConflict(DbUpdateConcurrencyException exception)
    {
        uint expected = 0;
        uint actual = 0;
        var entry = exception.Entries.Count > 0 ? exception.Entries[0] : null;
        if (entry is not null)
        {
            var tokenProperty = entry.Metadata.FindProperty(nameof(ContentEntryEntity.ConcurrencyToken));
            if (tokenProperty is not null)
            {
                expected = ToUInt32(entry.Property(nameof(ContentEntryEntity.ConcurrencyToken)).OriginalValue);
                var databaseValues = entry.GetDatabaseValues();
                if (databaseValues is not null)
                {
                    actual = ToUInt32(databaseValues[nameof(ContentEntryEntity.ConcurrencyToken)]);
                }
            }
        }

        return new ConcurrencyConflictException(expected, actual);
    }

    private static uint ToUInt32(object? value) =>
        value switch
        {
            uint unsigned => unsigned,
            long signed => checked((uint)signed),
            int integer => checked((uint)integer),
            _ => 0,
        };
}
