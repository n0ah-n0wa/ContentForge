namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Identity;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class EfContentPreviewTokenRepository(AppDbContext dbContext) : IContentPreviewTokenRepository
{
    public async Task<ContentPreviewTokenIssueResult> IssueAsync(
        ContentEntryId contentEntryId,
        UserId createdBy,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var token = JwtTokenService.GenerateOpaqueToken();
        var now = DateTimeOffset.UtcNow;
        var entity = new ContentPreviewTokenEntity
        {
            Id = Guid.NewGuid(),
            ContentEntryId = contentEntryId.Value,
            TokenHash = JwtTokenService.HashToken(token),
            CreatedBy = createdBy.Value,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

        dbContext.ContentPreviewTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ContentPreviewTokenIssueResult(token, expiresAt);
    }

    public async Task<ContentPreviewTokenRecord?> FindActiveAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = JwtTokenService.HashToken(token);
        var entity = await dbContext.ContentPreviewTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                previewToken => previewToken.TokenHash == tokenHash,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null || entity.RevokedAt is not null || entity.ExpiresAt <= now)
        {
            return null;
        }

        return new ContentPreviewTokenRecord(entity.Id, entity.ContentEntryId, entity.ExpiresAt);
    }

    public async Task RevokeActiveForEntryAsync(
        ContentEntryId contentEntryId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await dbContext.ContentPreviewTokens
            .Where(token =>
                token.ContentEntryId == contentEntryId.Value
                && token.RevokedAt == null
                && token.ExpiresAt > revokedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = revokedAt;
        }

        if (activeTokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
