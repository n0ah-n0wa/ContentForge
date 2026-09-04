namespace ContentForge.Infrastructure.Observability;

using Azure.Monitor.OpenTelemetry.AspNetCore;
using ContentForge.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

/// <summary>
/// Registers Azure Application Insights export via the OpenTelemetry distro.
/// </summary>
public static class ApplicationInsightsServiceCollectionExtensions
{
    /// <summary>
    /// Enables Azure Monitor telemetry export when environment and configuration allow it.
    /// </summary>
    public static IHostApplicationBuilder AddContentForgeApplicationInsights(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Configuration
            .GetSection(ApplicationInsightsOptions.SectionName)
            .Get<ApplicationInsightsOptions>() ?? new ApplicationInsightsOptions();

        var connectionString = ApplicationInsightsConnectionResolver.Resolve(builder.Configuration, options);
        if (!ApplicationInsightsConnectionResolver.ShouldEnable(builder.Environment, options, connectionString))
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor(monitorOptions =>
            {
                monitorOptions.ConnectionString = connectionString;
                monitorOptions.SamplingRatio = (float)Math.Clamp(options.SamplingRatio, 0d, 1d);
            })
            .WithTracing(tracerBuilder =>
            {
                // Must register EF instrumentation during service configuration (not after
                // ServiceProvider creation) or OpenTelemetry throws NotSupportedException.
                tracerBuilder.AddEntityFrameworkCoreInstrumentation(efOptions =>
                {
                    efOptions.SetDbStatementForText = false;
                    efOptions.SetDbStatementForStoredProcedure = false;
                });
                tracerBuilder.AddProcessor(new SensitiveTelemetryActivityProcessor());
            });

        ConfigureAspNetCoreAndHttpInstrumentation(builder.Services);
        ConfigureLogging(builder.Services);
        return builder;
    }

    private static void ConfigureAspNetCoreAndHttpInstrumentation(IServiceCollection services)
    {
        services.Configure<AspNetCoreTraceInstrumentationOptions>(instrumentationOptions =>
        {
            instrumentationOptions.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

            instrumentationOptions.EnrichWithHttpRequest = (activity, request) =>
            {
                if (request.HttpContext.Items.TryGetValue(HttpAuditRequestContext.CorrelationIdItemKey, out var correlationId)
                    && correlationId is string correlationIdValue)
                {
                    activity.SetTag("correlationId", correlationIdValue);
                }

                var userId = request.HttpContext.User.FindFirst("sub")?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    activity.SetTag("userId", userId);
                }
            };
        });

        services.Configure<HttpClientTraceInstrumentationOptions>(instrumentationOptions =>
        {
            instrumentationOptions.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                if (request.RequestUri is not null)
                {
                    activity.SetTag("http.url", SensitiveTelemetryRedactor.Redact(request.RequestUri.ToString()));
                }
            };
        });
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        services.Configure<LoggerFilterOptions>(options =>
        {
            options.AddFilter("Azure", LogLevel.Warning);
        });

        services.ConfigureOpenTelemetryLoggerProvider((_, loggerBuilder) =>
        {
            loggerBuilder.AddProcessor(new SensitiveTelemetryLogRecordProcessor());
        });
    }
}

/// <summary>
/// Resolves Application Insights connection settings.
/// </summary>
internal static class ApplicationInsightsConnectionResolver
{
    internal static bool ShouldEnable(
        IHostEnvironment environment,
        ApplicationInsightsOptions options,
        string? connectionString)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        if (options.Enabled)
        {
            return true;
        }

        return environment.IsProduction() || environment.IsEnvironment("Staging");
    }

    internal static string? Resolve(IConfiguration configuration, ApplicationInsightsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString.Trim();
        }

        var environmentVariable = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        return string.IsNullOrWhiteSpace(environmentVariable) ? null : environmentVariable.Trim();
    }
}
