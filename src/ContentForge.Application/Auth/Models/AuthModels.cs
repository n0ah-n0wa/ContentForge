namespace ContentForge.Application.Auth.Models;

using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

/// <summary>
/// Credentials submitted for authentication.
/// </summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Request to invalidate the current authentication session.
/// User identity is taken from the authenticated principal — callers must not supply a target user id.
/// </summary>
public sealed record LogoutRequest(Guid UserId, string? RefreshToken = null);

/// <summary>
/// HTTP body for logout. Only a refresh token may be supplied; the session owner is the caller.
/// </summary>
public sealed record LogoutApiRequest(string? RefreshToken = null);

/// <summary>
/// Request to obtain a new access token using a refresh token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Initiates a password reset for the supplied email address.
/// </summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>
/// Completes a password reset using an identity-issued reset token.
/// </summary>
public sealed record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

/// <summary>
/// Issued authentication tokens and user context.
/// </summary>
public sealed record AuthenticationResult(
    UserId UserId,
    string Email,
    string DisplayName,
    RoleName Role,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAt = null)
{
    public LoginResultDto ToLoginResultDto() =>
        new(
            UserId.Value,
            Email,
            DisplayName,
            Role,
            AccessToken,
            AccessTokenExpiresAt,
            RefreshToken,
            RefreshTokenExpiresAt);
}

/// <summary>
/// Authenticated user context returned to callers after login.
/// </summary>
public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    RoleName Role);

/// <summary>
/// Complete login response including issued tokens and user context.
/// </summary>
public sealed record LoginResultDto(
    Guid UserId,
    string Email,
    string DisplayName,
    RoleName Role,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAt);
