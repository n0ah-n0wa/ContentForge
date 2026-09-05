namespace ContentForge.Api.Infrastructure;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class ForwardedHeadersExtensions
{
    internal static IServiceCollection AddContentForgeForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!ShouldUseForwardedHeaders(environment))
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Only the immediate reverse proxy hop is trusted. Client-supplied XFF must be
            // overwritten by nginx (see infra/docker/web/nginx.conf.template).
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = false;

            // Start from defaults (loopback) — do NOT clear to "trust all".
            // Additional known proxies/CIDRs come from configuration.
            var section = configuration.GetSection("ForwardedHeaders");
            foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
            {
                if (TryParseCidr(network, out var parsed))
                {
                    options.KnownNetworks.Add(parsed);
                }
            }
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

    private static bool TryParseCidr(string value, out Microsoft.AspNetCore.HttpOverrides.IPNetwork network)
    {
        network = null!;
        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        network = new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength);
        return true;
    }
}
