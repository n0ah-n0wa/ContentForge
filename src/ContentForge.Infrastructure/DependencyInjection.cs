using Microsoft.Extensions.DependencyInjection;

namespace ContentForge.Infrastructure;

/// <summary>
/// Dependency injection registration for infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
