namespace ContentForge.Application.Abstractions;

using ContentForge.Domain.Common;

/// <summary>
/// Invalidates authentication sessions for a user (refresh tokens and access-token binding).
/// </summary>
public interface ISessionInvalidationService
{
    Task InvalidateUserSessionsAsync(UserId userId, CancellationToken cancellationToken = default);
}
