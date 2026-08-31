namespace ContentForge.Api.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class ForwardedHeadersExtensions
{
    internal static IServiceCollection AddContentForgeForwardedHeaders(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        if (!ShouldUseForwardedHeaders(environment))
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    internal static WebApplication UseContentForgeForwardedHeaders(this WebApplication app)
    {
        if (ShouldUseForwardedHeaders(app.Environment))
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    private static bool ShouldUseForwardedHeaders(IHostEnvironment environment) =>
        !environment.IsDevelopment()
        && !environment.IsEnvironment("Testing");
}
