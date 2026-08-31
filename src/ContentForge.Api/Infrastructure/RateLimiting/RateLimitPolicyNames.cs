namespace ContentForge.Api.Infrastructure.RateLimiting;

/// <summary>
/// Named ASP.NET Core rate limiter policies applied to abuse-sensitive endpoints.
/// </summary>
internal static class RateLimitPolicyNames
{
    internal const string AuthLogin = "auth-login";

    internal const string AuthRefresh = "auth-refresh";

    internal const string PasswordResetRequest = "password-reset-request";

    internal const string PasswordResetConfirm = "password-reset-confirm";

    internal const string PublicApi = "public-api";

    internal const string MediaUpload = "media-upload";

    internal const string ContentPreview = "content-preview";
}
