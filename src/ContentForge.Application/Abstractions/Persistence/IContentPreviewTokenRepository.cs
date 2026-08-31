namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.ContentPreview.Models;
using ContentForge.Domain.Common;

/// <summary>
/// Persistence port for scoped preview access tokens.
/// </summary>
public interface IContentPreviewTokenRepository
{
    Task<ContentPreviewTokenIssueResult> IssueAsync(
        ContentEntryId contentEntryId,
        UserId createdBy,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<ContentPreviewTokenRecord?> FindActiveAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task RevokeActiveForEntryAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}
