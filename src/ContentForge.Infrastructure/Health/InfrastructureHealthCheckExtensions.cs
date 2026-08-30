namespace ContentForge.Infrastructure.Health;

using ContentForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Registers infrastructure dependency health checks.
/// </summary>
public static class InfrastructureHealthCheckExtensions
{
    /// <summary>
    /// Adds database and media storage readiness checks.
    /// </summary>
    public static IHealthChecksBuilder AddContentForgeInfrastructureHealthChecks(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: [HealthCheckTags.Ready])
            .AddCheck<MediaStorageHealthCheck>(
                name: "storage",
                failureStatus: HealthStatus.Unhealthy,
                tags: [HealthCheckTags.Ready]);
    }
}
