namespace ContentForge.UnitTests.Infrastructure;

using ContentForge.Infrastructure.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

public sealed class ApplicationInsightsConnectionResolverTests
{
    [Fact]
    public void ShouldEnable_ReturnsFalse_InTestingEnvironment()
    {
        var environment = new TestHostEnvironment("Testing");
        var options = new ApplicationInsightsOptions { Enabled = true };

        ApplicationInsightsConnectionResolver.ShouldEnable(
                environment,
                options,
                "InstrumentationKey=test;IngestionEndpoint=https://example.com/")
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEnable_ReturnsFalse_WhenConnectionStringMissing()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var options = new ApplicationInsightsOptions { Enabled = true };

        ApplicationInsightsConnectionResolver.ShouldEnable(environment, options, connectionString: null)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEnable_ReturnsTrue_InProduction_WhenConnectionStringConfigured()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var options = new ApplicationInsightsOptions();

        ApplicationInsightsConnectionResolver.ShouldEnable(
                environment,
                options,
                "InstrumentationKey=test;IngestionEndpoint=https://example.com/")
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldEnable_ReturnsTrue_InStaging_WhenConnectionStringConfigured()
    {
        var environment = new TestHostEnvironment("Staging");
        var options = new ApplicationInsightsOptions();

        ApplicationInsightsConnectionResolver.ShouldEnable(
                environment,
                options,
                "InstrumentationKey=test;IngestionEndpoint=https://example.com/")
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldEnable_ReturnsFalse_InDevelopment_WhenNotExplicitlyEnabled()
    {
        var environment = new TestHostEnvironment(Environments.Development);
        var options = new ApplicationInsightsOptions();

        ApplicationInsightsConnectionResolver.ShouldEnable(
                environment,
                options,
                "InstrumentationKey=test;IngestionEndpoint=https://example.com/")
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldEnable_ReturnsTrue_InDevelopment_WhenExplicitlyEnabled()
    {
        var environment = new TestHostEnvironment(Environments.Development);
        var options = new ApplicationInsightsOptions { Enabled = true };

        ApplicationInsightsConnectionResolver.ShouldEnable(
                environment,
                options,
                "InstrumentationKey=test;IngestionEndpoint=https://example.com/")
            .Should().BeTrue();
    }

    [Fact]
    public void Resolve_PrefersConfiguredValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:ConnectionString"] = "ConfiguredConnectionString",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "EnvironmentConnectionString",
            })
            .Build();

        var options = new ApplicationInsightsOptions
        {
            ConnectionString = "ConfiguredConnectionString",
        };

        ApplicationInsightsConnectionResolver.Resolve(configuration, options)
            .Should().Be("ConfiguredConnectionString");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "ContentForge.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
