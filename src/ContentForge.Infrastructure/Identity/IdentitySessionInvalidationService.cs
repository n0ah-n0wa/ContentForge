namespace ContentForge.Infrastructure.Identity;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

internal sealed class IdentitySessionInvalidationService(
    UserManager<ContentForgeUser> userManager,
    RefreshTokenService refreshTokenService,
    AppDbContext dbContext) : ISessionInvalidationService
{
    public async Task InvalidateUserSessionsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.Value.ToString()).ConfigureAwait(false);
        if (user is not null)
        {
            await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        }

        await refreshTokenService.RevokeAllForUserAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
