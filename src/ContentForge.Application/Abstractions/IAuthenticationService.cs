namespace ContentForge.Application.Abstractions;

using ContentForge.Application.Auth.Models;

/// <summary>
/// Authentication port implemented by infrastructure using ASP.NET Core Identity and JWT.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);
}
