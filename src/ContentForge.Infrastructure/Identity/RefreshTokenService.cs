namespace ContentForge.Infrastructure.Identity;

using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class RefreshTokenService(AppDbContext dbContext, TimeProvider timeProvider)
{
    internal async Task<(string Token, DateTimeOffset ExpiresAt)> IssueAsync(
        Guid userId,
        int lifetimeDays,
        CancellationToken cancellationToken)
    {
        var token = JwtTokenService.GenerateRefreshToken();
        var entity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = JwtTokenService.HashToken(token),
            CreatedAt = timeProvider.GetUtcNow(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(lifetimeDays),
        };

        await dbContext.RefreshTokens.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return (token, entity.ExpiresAt);
    }

    internal async Task<RefreshTokenEntity?> FindByHashAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = JwtTokenService.HashToken(refreshToken);
        return await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task<RefreshTokenEntity?> FindActiveAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await FindByHashAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        if (token is null || token.RevokedAt is not null)
        {
            return null;
        }

        return token;
    }

    internal Task RevokeAsync(RefreshTokenEntity token, string? replacedByTokenHash, CancellationToken cancellationToken)
    {
        token.RevokedAt = timeProvider.GetUtcNow();
        token.ReplacedByTokenHash = replacedByTokenHash;
        return Task.CompletedTask;
    }

    internal async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }
}
