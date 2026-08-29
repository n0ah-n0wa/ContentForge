namespace ContentForge.Application.Auth.Models;

using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

/// <summary>
/// Credentials submitted for authentication.
/// </summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Request to invalidate the current authentication session.
/// </summary>
public sealed record LogoutRequest(UserId UserId, string? RefreshToken = null);

/// <summary>
/// Request to obtain a new access token using a refresh token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);

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
    DateTimeOffset? RefreshTokenExpiresAt = null);

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
