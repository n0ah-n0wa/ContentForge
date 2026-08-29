namespace ContentForge.Application.Users.Models;

using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;

/// <summary>
/// Application representation of a CMS user account.
/// </summary>
public sealed record UserAccount(
    UserId Id,
    string Email,
    string DisplayName,
    string PasswordHash,
    bool IsActive,
    RoleName Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt)
{
    public UserAccount Disable(DateTimeOffset updatedAt) =>
        this with { IsActive = false, UpdatedAt = updatedAt };

    public UserAccount Enable(DateTimeOffset updatedAt) =>
        this with { IsActive = true, UpdatedAt = updatedAt };

    public UserAccount WithRole(RoleName role, DateTimeOffset updatedAt) =>
        this with { Role = role, UpdatedAt = updatedAt };

    public UserAccount WithDisplayName(string displayName, DateTimeOffset updatedAt) =>
        this with { DisplayName = displayName.Trim(), UpdatedAt = updatedAt };

    public UserAccount RecordLogin(DateTimeOffset timestamp) =>
        this with { LastLoginAt = timestamp, UpdatedAt = timestamp };
}

/// <summary>
/// User account details returned by administrative queries.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    RoleName Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt);

/// <summary>
/// Role definition returned by administrative queries.
/// </summary>
public sealed record RoleDto(RoleName Name, IReadOnlyList<string> Permissions);
