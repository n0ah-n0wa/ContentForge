namespace ContentForge.Application.Abstractions;

/// <summary>
/// Delivers a password-reset token to the account holder without revealing whether the email exists to API callers.
/// </summary>
public interface IPasswordResetNotifier
{
    Task NotifyAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default);
}
