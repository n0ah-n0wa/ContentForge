namespace ContentForge.Api.Infrastructure.RateLimiting;

using System.Threading.RateLimiting;
using ContentForge.Api.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

internal static class RateLimitingServiceCollectionExtensions
{
    internal static IServiceCollection AddContentForgeRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers.RetryAfter =
                        retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var problem = ProblemDetailsFactory.Create(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "Too Many Requests",
                    "Rate limit exceeded. Retry later.",
                    ApiConstants.ErrorTypes.RateLimit);
                await ProblemDetailsFactory.WriteAsync(context.HttpContext, problem, cancellationToken)
                    .ConfigureAwait(false);
            };

            options.AddPolicy(
                RateLimitPolicyNames.AuthLogin,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.AuthLogin, partitionByUser: false));
            options.AddPolicy(
                RateLimitPolicyNames.AuthRefresh,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.AuthRefresh, partitionByUser: false));
            options.AddPolicy(
                RateLimitPolicyNames.PasswordResetRequest,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.PasswordResetRequest, partitionByUser: false));
            options.AddPolicy(
                RateLimitPolicyNames.PasswordResetConfirm,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.PasswordResetConfirm, partitionByUser: false));
            options.AddPolicy(
                RateLimitPolicyNames.PublicApi,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.PublicApi, partitionByUser: false));
            options.AddPolicy(
                RateLimitPolicyNames.MediaUpload,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.MediaUpload, partitionByUser: true));
            options.AddPolicy(
                RateLimitPolicyNames.ContentPreview,
                httpContext => CreatePartition(httpContext, RateLimitPolicyNames.ContentPreview, partitionByUser: false));
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        string policyName,
        bool partitionByUser)
    {
        var globalOptions = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
        var policyOptions = ResolvePolicyOptions(globalOptions, policyName);
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = ResolvePermitLimit(policyOptions, globalOptions, environment, config, policyName);
        var window = TimeSpan.FromSeconds(Math.Max(1, policyOptions.WindowSeconds));
        var partitionKey = ResolvePartitionKey(httpContext, partitionByUser && policyOptions.PartitionByAuthenticatedUser);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
            });
    }

    private static RateLimitPolicyOptions ResolvePolicyOptions(RateLimitingOptions options, string policyName) =>
        policyName switch
        {
            RateLimitPolicyNames.AuthLogin => options.AuthLogin,
            RateLimitPolicyNames.AuthRefresh => options.AuthRefresh,
            RateLimitPolicyNames.PasswordResetRequest => options.PasswordResetRequest,
            RateLimitPolicyNames.PasswordResetConfirm => options.PasswordResetConfirm,
            RateLimitPolicyNames.PublicApi => options.PublicApi,
            RateLimitPolicyNames.MediaUpload => options.MediaUpload,
            RateLimitPolicyNames.ContentPreview => options.ContentPreview,
            _ => throw new InvalidOperationException($"Unknown rate limit policy '{policyName}'."),
        };

    private static int ResolvePermitLimit(
        RateLimitPolicyOptions policyOptions,
        RateLimitingOptions globalOptions,
        IHostEnvironment environment,
        IConfiguration configuration,
        string policyName)
    {
        var overrideKey = $"{RateLimitingOptions.SectionName}:{MapPolicyConfigKey(policyName)}:PermitLimit";
        var configuredOverride = configuration.GetValue<int?>(overrideKey);
        if (configuredOverride.HasValue)
        {
            return Math.Max(1, configuredOverride.Value);
        }

        if (!globalOptions.Enabled || environment.IsEnvironment("Testing"))
        {
            return Math.Max(1, globalOptions.DisabledPermitLimit);
        }

        return Math.Max(1, policyOptions.PermitLimit);
    }

    private static string ResolvePartitionKey(HttpContext httpContext, bool partitionByUser)
    {
        if (partitionByUser)
        {
            var userId = httpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"}";
    }

    private static string MapPolicyConfigKey(string policyName) =>
        policyName switch
        {
            RateLimitPolicyNames.AuthLogin => nameof(RateLimitingOptions.AuthLogin),
            RateLimitPolicyNames.AuthRefresh => nameof(RateLimitingOptions.AuthRefresh),
            RateLimitPolicyNames.PasswordResetRequest => nameof(RateLimitingOptions.PasswordResetRequest),
            RateLimitPolicyNames.PasswordResetConfirm => nameof(RateLimitingOptions.PasswordResetConfirm),
            RateLimitPolicyNames.PublicApi => nameof(RateLimitingOptions.PublicApi),
            RateLimitPolicyNames.MediaUpload => nameof(RateLimitingOptions.MediaUpload),
            RateLimitPolicyNames.ContentPreview => nameof(RateLimitingOptions.ContentPreview),
            _ => policyName,
        };
}
