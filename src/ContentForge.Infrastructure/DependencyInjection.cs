namespace ContentForge.Infrastructure;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Repositories;
using ContentForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Dependency injection registration for infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddDbContext<AppDbContext>((_, options) =>
        {
            var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                ?? throw new InvalidOperationException(
                    $"Database configuration section '{DatabaseOptions.SectionName}' is missing.");

            if (string.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured.");
            }

            DesignTimeDbContextFactory.ConfigureProvider(options, databaseOptions);
        });

        services.AddContentForgeAuthentication(configuration, environment);
        services.AddApplicationPortStubs();
        services.AddFileStorage(configuration, environment);
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IAuditRequestContext, HttpAuditRequestContext>();
        services.AddScoped<IAuditService, EfAuditService>();
        services.AddScoped<IContentSearchService, EfContentSearchService>();
        services.AddScoped<IContentTypeRepository, EfContentTypeRepository>();
        services.AddScoped<IContentEntryRepository, EfContentEntryRepository>();
        services.AddScoped<IMediaRepository, EfMediaRepository>();
        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IDashboardReadService, EfDashboardReadService>();

        return services;
    }
}
