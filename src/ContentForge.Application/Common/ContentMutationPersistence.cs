namespace ContentForge.Application.Common;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;

/// <summary>
/// Persists a content aggregate mutation and its audit event in a single transaction.
/// </summary>
internal static class ContentMutationPersistence
{
    internal static async Task PersistAsync(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ContentEntry entry,
        AuditAction action,
        UserId userId,
        CancellationToken cancellationToken,
        string? metadata = null)
    {
        await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                try
                {
                    await repository.UpdateAsync(entry, token).ConfigureAwait(false);
                    await ApplicationGuard.SaveChangesAsync(unitOfWork, token).ConfigureAwait(false);
                    await auditService.RecordAsync(
                        action,
                        "ContentEntry",
                        entry.Id.Value.ToString(),
                        userId,
                        metadata: metadata,
                        cancellationToken: token).ConfigureAwait(false);
                }
                catch (ConcurrencyConflictException exception)
                {
                    throw ApplicationGuard.ToConcurrencyConflict(exception);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
