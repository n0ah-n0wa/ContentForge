namespace ContentForge.Infrastructure.Identity;

using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

internal sealed class IdentityPasswordHasher(UserManager<ContentForgeUser> userManager) : IPasswordHasher
{
    public string HashPassword(string password) =>
        userManager.PasswordHasher.HashPassword(new ContentForgeUser(), password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = userManager.PasswordHasher.VerifyHashedPassword(
            new ContentForgeUser(),
            passwordHash,
            password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
