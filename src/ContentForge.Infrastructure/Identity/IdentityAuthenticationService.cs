namespace ContentForge.Infrastructure.Identity;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

internal sealed class IdentityAuthenticationService(
    UserManager<ContentForgeUser> userManager,
    AppDbContext dbContext,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .Include(account => account.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(account => account.NormalizedEmail == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            throw new AuthenticationFailedException();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
        {
            await userManager.AccessFailedAsync(user).ConfigureAwait(false);
            throw new AuthenticationFailedException();
        }

        await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        user.LastLoginAt = now;
        user.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await IssueTokensAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        await refreshTokenService.RevokeAllForUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is not null)
        {
            await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await refreshTokenService.FindByHashAsync(request.RefreshToken, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AuthenticationFailedException();

        if (existingToken.RevokedAt is not null)
        {
            // Reuse of a rotated/revoked refresh token — revoke the entire family.
            await refreshTokenService.RevokeAllForUserAsync(existingToken.UserId, cancellationToken)
                .ConfigureAwait(false);

            var compromisedUser = await userManager.FindByIdAsync(existingToken.UserId.ToString())
                .ConfigureAwait(false);
            if (compromisedUser is not null)
            {
                await userManager.UpdateSecurityStampAsync(compromisedUser).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailedException();
        }

        if (existingToken.ExpiresAt <= timeProvider.GetUtcNow())
        {
            await refreshTokenService.RevokeAsync(existingToken, replacedByTokenHash: null, cancellationToken)
                .ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailedException();
        }

        var user = await dbContext.Users
            .Include(account => account.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(account => account.Id == existingToken.UserId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AuthenticationFailedException();

        if (!user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var role = ResolveRole(user);
        var (accessToken, accessTokenExpiresAt) = jwtTokenService.CreateAccessToken(user, role);
        var (refreshToken, refreshTokenExpiresAt) = await refreshTokenService.IssueAsync(
            user.Id,
            jwtOptions.Value.RefreshTokenLifetimeDays,
            cancellationToken).ConfigureAwait(false);

        await refreshTokenService.RevokeAsync(
                existingToken,
                JwtTokenService.HashToken(refreshToken),
                cancellationToken)
            .ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult(
            UserId.From(user.Id),
            user.Email ?? string.Empty,
            user.DisplayName,
            role,
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private async Task<AuthenticationResult> IssueTokensAsync(
        ContentForgeUser user,
        CancellationToken cancellationToken)
    {
        var role = ResolveRole(user);
        var (accessToken, accessTokenExpiresAt) = jwtTokenService.CreateAccessToken(user, role);
        var (refreshToken, refreshTokenExpiresAt) = await refreshTokenService.IssueAsync(
            user.Id,
            jwtOptions.Value.RefreshTokenLifetimeDays,
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult(
            UserId.From(user.Id),
            user.Email ?? string.Empty,
            user.DisplayName,
            role,
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private static RoleName ResolveRole(ContentForgeUser user)
    {
        var roleName = user.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault()
            ?? RoleName.Viewer.ToString();
        return Enum.Parse<RoleName>(roleName);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
