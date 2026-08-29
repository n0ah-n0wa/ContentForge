namespace ContentForge.Infrastructure.Identity;

using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

internal sealed class IdentityPasswordResetService(
    UserManager<ContentForgeUser> userManager,
    AppDbContext dbContext) : IPasswordResetService
{
    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            return;
        }

        _ = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
    }

    public async Task ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Password reset failed.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Password reset failed.");
        }

        var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Password reset failed.");
        }
    }
}
