namespace ContentForge.Application.Users.Models;

/// <summary>
/// API request to create a user account.
/// </summary>
public sealed record CreateUserApiRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role);
