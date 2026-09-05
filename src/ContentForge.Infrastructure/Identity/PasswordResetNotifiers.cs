namespace ContentForge.Infrastructure.Identity;

using System.Collections.Concurrent;
using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Test/Development sink that retains the latest reset token per email.
/// </summary>
public sealed class CapturingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ConcurrentDictionary<string, string> _tokensByEmail = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Snapshot => _tokensByEmail;

    public Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        _tokensByEmail[email.Trim()] = resetToken;
        return Task.CompletedTask;
    }

    public bool TryGetToken(string email, out string? token) =>
        _tokensByEmail.TryGetValue(email.Trim(), out token);
}

internal sealed class LoggingPasswordResetNotifier(
    ILogger<LoggingPasswordResetNotifier> logger,
    IOptions<PasswordResetOptions> options) : IPasswordResetNotifier
{
    public Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = options.Value.PublicAppBaseUrl?.TrimEnd('/');
        var link = string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

        PasswordResetNotifierLog.TokenGenerated(logger, email, resetToken, link);
        return Task.CompletedTask;
    }
}

internal sealed class SmtpPasswordResetNotifier(
    IOptions<PasswordResetOptions> options,
    ILogger<SmtpPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public async Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var smtp = settings.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromAddress))
        {
            throw new InvalidOperationException("PasswordReset SMTP Host and FromAddress are required.");
        }

        var baseUrl = settings.PublicAppBaseUrl?.TrimEnd('/')
            ?? throw new InvalidOperationException("PasswordReset:PublicAppBaseUrl is required for SMTP delivery.");

        var link =
            $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

        using var message = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(smtp.FromAddress, smtp.FromDisplayName ?? "ContentForge"),
            Subject = "ContentForge password reset",
            Body =
                $"A password reset was requested for your ContentForge account.{Environment.NewLine}{Environment.NewLine}" +
                $"Reset link:{Environment.NewLine}{link}{Environment.NewLine}{Environment.NewLine}" +
                $"If you did not request this, you can ignore this message.",
            IsBodyHtml = false,
        };
        message.To.Add(email);

        using var client = new System.Net.Mail.SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Credentials = new System.Net.NetworkCredential(smtp.Username, smtp.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CA2016 // SmtpClient.SendMailAsync has no CancellationToken overload on this TFM
        await client.SendMailAsync(message).ConfigureAwait(false);
#pragma warning restore CA2016
        PasswordResetNotifierLog.SmtpAccepted(logger, email);
    }
}

internal sealed class CompositePasswordResetNotifier(params IPasswordResetNotifier[] notifiers)
    : IPasswordResetNotifier
{
    public async Task NotifyAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        foreach (var notifier in notifiers)
        {
            await notifier.NotifyAsync(email, resetToken, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal static partial class PasswordResetNotifierLog
{
    [LoggerMessage(
        EventId = 6101,
        Level = LogLevel.Warning,
        Message = "Password reset token generated for {Email}. DeliveryMode=Logging. Token={Token}. ResetLink={ResetLink}")]
    public static partial void TokenGenerated(ILogger logger, string email, string token, string? resetLink);

    [LoggerMessage(
        EventId = 6102,
        Level = LogLevel.Information,
        Message = "Password reset email accepted by SMTP for {Email}")]
    public static partial void SmtpAccepted(ILogger logger, string email);
}
