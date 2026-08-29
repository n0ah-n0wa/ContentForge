namespace ContentForge.Application.Abstractions;

using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

/// <summary>
/// Provides access to the authenticated user context for the current operation.
/// </summary>
public interface ICurrentUserService
{
    UserId? UserId { get; }

    RoleDefinition? Role { get; }

    string? Email { get; }

    string? DisplayName { get; }

    bool IsAuthenticated { get; }
}
