namespace ContentForge.Infrastructure.Options;

/// <summary>
/// Password reset delivery settings. Production requires SMTP; Development/Testing may use logging capture.
/// </summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>
    /// <c>Logging</c> (non-production only) or <c>Smtp</c> (required in Production).
    /// </summary>
    public string DeliveryMode { get; set; } = "Logging";

    /// <summary>
    /// Public admin origin used to build reset links in notifications (no trailing slash).
    /// </summary>
    public string? PublicAppBaseUrl { get; set; }

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string? FromDisplayName { get; set; } = "ContentForge";
}
