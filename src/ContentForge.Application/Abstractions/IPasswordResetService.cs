namespace ContentForge.Application.Abstractions;

/// <summary>
/// Password reset workflow port implemented by infrastructure using ASP.NET Core Identity.
/// </summary>
public interface IPasswordResetService
{
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);
}
