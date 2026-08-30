namespace ContentForge.Api.Infrastructure;

using System.Text.Json;
using ContentForge.Infrastructure.Health;
using ContentForge.Infrastructure.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Registers API observability services.
/// </summary>
internal static class ObservabilityServiceCollectionExtensions
{
    internal static WebApplicationBuilder AddContentForgeObservability(this WebApplicationBuilder builder)
    {
        ConfigureStructuredLogging(builder);
        builder.AddContentForgeApplicationInsights();
        builder.Services.AddHealthChecks()
            .AddContentForgeInfrastructureHealthChecks();

        return builder;
    }

    internal static WebApplication UseContentForgeObservability(this WebApplication app)
    {
        app.MapHealthChecks(ObservabilityConstants.LiveHealthEndpoint, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriter.WriteMinimalAsync,
        });

        app.MapHealthChecks(ObservabilityConstants.ReadyHealthEndpoint, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = HealthCheckResponseWriter.WriteMinimalAsync,
        });

        return app;
    }

    private static void ConfigureStructuredLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "O";
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false,
            };
        });
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    }
}
