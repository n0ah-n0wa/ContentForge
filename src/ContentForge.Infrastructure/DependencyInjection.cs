namespace ContentForge.Infrastructure;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Caching;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Application.ContentPreview;
using ContentForge.Application.PublicContent.Caching;
using ContentForge.Application.Scheduling;
using ContentForge.Infrastructure.Caching;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using ContentForge.Infrastructure.Persistence.Repositories;
using ContentForge.Infrastructure.Scheduling;
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
        services.AddScoped<IScheduledJobRepository, EfScheduledJobRepository>();
        services.AddScoped<IContentPreviewTokenRepository, EfContentPreviewTokenRepository>();
        services.AddScoped<IBackgroundJobScheduler, BackgroundJobScheduler>();
        services.AddScoped<IScheduledJobProcessor, ScheduledJobProcessor>();
        services.Configure<ScheduledPublishingOptions>(configuration.GetSection(ScheduledPublishingOptions.SectionName));
        services.Configure<ContentPreviewOptions>(configuration.GetSection(ContentPreviewOptions.SectionName));
        services.Configure<PublicContentCacheOptions>(configuration.GetSection(PublicContentCacheOptions.SectionName));

        services.AddMemoryCache(options =>
        {
            var cacheOptions = configuration.GetSection(PublicContentCacheOptions.SectionName).Get<PublicContentCacheOptions>()
                ?? new PublicContentCacheOptions();
            options.SizeLimit = Math.Max(1, cacheOptions.MaxEntries);
        });

        services.AddSingleton<MemoryPublicContentCache>();
        services.AddSingleton<IPublicContentCache>(sp => sp.GetRequiredService<MemoryPublicContentCache>());
        services.AddSingleton<IPublicContentCacheStatistics>(sp => sp.GetRequiredService<MemoryPublicContentCache>());
        services.AddScoped<IPublicContentCacheInvalidator, PublicContentCacheInvalidator>();

        if (environment?.IsEnvironment("Testing") != true)
        {
            services.AddHostedService<ScheduledPublishingBackgroundService>();
        }

        if (environment?.IsDevelopment() == true)
        {
            services.AddHostedService<Persistence.Development.DevelopmentDatabaseInitializer>();
        }

        return services;
    }
}
