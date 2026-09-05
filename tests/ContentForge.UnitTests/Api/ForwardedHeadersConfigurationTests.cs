namespace ContentForge.UnitTests.Api;

using ContentForge.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class ForwardedHeadersConfigurationTests
{
    [Fact]
    public void Production_DoesNotTrustAllProxies_AndLimitsForwardHops()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5",
                ["ForwardedHeaders:KnownNetworks:0"] = "10.10.0.0/16",
            })
            .Build();

        var services = new ServiceCollection();
        var environment = new HostingEnvironment { EnvironmentName = Environments.Production };
        services.AddContentForgeForwardedHeaders(configuration, environment);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardLimit.Should().Be(1);
        options.KnownProxies.Should().Contain(System.Net.IPAddress.Parse("10.0.0.5"));
        options.KnownProxies.Should().Contain(proxy => System.Net.IPAddress.IsLoopback(proxy));
        options.KnownNetworks.Should().Contain(network => network.Prefix.ToString() == "10.10.0.0");
    }

    private sealed class HostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "ContentForge.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
