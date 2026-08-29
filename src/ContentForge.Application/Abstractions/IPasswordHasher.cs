namespace ContentForge.Application.Abstractions;

/// <summary>
/// Hashes and verifies user passwords without exposing hashing algorithms to the application layer.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
