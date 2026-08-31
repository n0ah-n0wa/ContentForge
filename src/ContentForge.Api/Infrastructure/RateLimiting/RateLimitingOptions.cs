namespace ContentForge.Api.Infrastructure.RateLimiting;

/// <summary>
/// Configurable rate limiting for abuse-sensitive API endpoints.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// When false, all policies use <see cref="DisabledPermitLimit"/> (effectively unlimited for normal use).
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// High permit limit used when disabled or in the Testing environment (unless a policy override is configured).
    /// </summary>
    public int DisabledPermitLimit { get; init; } = 10_000;

    public RateLimitPolicyOptions AuthLogin { get; init; } = new()
    {
        PermitLimit = 15,
        WindowSeconds = 60,
    };

    public RateLimitPolicyOptions AuthRefresh { get; init; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
    };

    public RateLimitPolicyOptions PasswordResetRequest { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
    };

    public RateLimitPolicyOptions PasswordResetConfirm { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
    };

    public RateLimitPolicyOptions PublicApi { get; init; } = new()
    {
        PermitLimit = 120,
        WindowSeconds = 60,
    };

    public RateLimitPolicyOptions MediaUpload { get; init; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60,
        PartitionByAuthenticatedUser = true,
    };

    public RateLimitPolicyOptions ContentPreview { get; init; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
    };
}

/// <summary>
/// Fixed-window limiter settings for a single policy.
/// </summary>
public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 60;

    public int WindowSeconds { get; init; } = 60;

    /// <summary>
    /// When true, authenticated callers are partitioned by JWT <c>sub</c>; anonymous callers use client IP.
    /// </summary>
    public bool PartitionByAuthenticatedUser { get; init; }
}
