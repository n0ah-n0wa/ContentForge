namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            // Parallel updates can hit unique indexes (e.g. content version number) before/alongside
            // the concurrency-token UPDATE, which surfaces as DbUpdateException instead of
            // DbUpdateConcurrencyException. Treat as an optimistic concurrency conflict.
            throw CreateConcurrencyConflictFromUniqueViolation(exception);
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

    private static ConcurrencyConflictException CreateConcurrencyConflictFromUniqueViolation(DbUpdateException exception)
    {
        uint expected = 0;
        uint actual = 0;
        EntityEntry? entry = null;
        foreach (var candidate in exception.Entries)
        {
            if (candidate.Entity is ContentEntryEntity)
            {
                entry = candidate;
                break;
            }
        }

        entry ??= exception.Entries.Count > 0 ? exception.Entries[0] : null;
        if (entry?.Entity is ContentEntryEntity contentEntry)
        {
            expected = ToUInt32(entry.Property(nameof(ContentEntryEntity.ConcurrencyToken)).OriginalValue);
            actual = checked((uint)contentEntry.ConcurrencyToken);
            var databaseValues = entry.GetDatabaseValues();
            if (databaseValues is not null)
            {
                actual = ToUInt32(databaseValues[nameof(ContentEntryEntity.ConcurrencyToken)]);
            }
        }

        return new ConcurrencyConflictException(expected, actual);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        // Only map content-version unique races to concurrency conflicts. Other unique
        // constraints (slugs, names, relations) must remain DbUpdateException for callers
        // and ConstraintPersistenceTests.
        const string contentVersionConstraint = "IX_ContentVersions_ContentEntryId_VersionNumber";

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(contentVersionConstraint, StringComparison.Ordinal))
            {
                return true;
            }

            var constraintName = current.GetType().GetProperty("ConstraintName")?.GetValue(current) as string;
            if (string.Equals(constraintName, contentVersionConstraint, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
