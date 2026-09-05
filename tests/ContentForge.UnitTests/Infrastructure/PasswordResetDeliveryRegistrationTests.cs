namespace ContentForge.UnitTests.Infrastructure;

using ContentForge.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public sealed class PasswordResetDeliveryRegistrationTests
{
    [Fact]
    public void Production_RejectsLoggingDeliveryMode()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "ContentForge",
                ["Jwt:Audience"] = "ContentForge.Admin",
                ["Jwt:SigningKey"] = "PRODUCTION_GRADE_SIGNING_KEY_32CHARS_MIN",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["PasswordReset:DeliveryMode"] = "Logging",
                ["Database:Provider"] = "PostgreSQL",
                ["Database:ConnectionString"] = "Host=localhost;Database=x;Username=x;Password=x",
            })
            .Build();

        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var act = () => services.AddInfrastructure(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Logging is not allowed in Production*");
    }

    [Fact]
    public void Staging_AllowsLoggingDeliveryMode()
    {
        var configuration = BuildBaseConfiguration(new Dictionary<string, string?>
        {
            ["PasswordReset:DeliveryMode"] = "Logging",
        });

        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment { EnvironmentName = "Staging" };

        var act = () => services.AddInfrastructure(configuration, environment);

        act.Should().NotThrow();
    }

    [Fact]
    public void Staging_StillRejectsPlaceholderJwtSigningKey()
    {
        var configuration = BuildBaseConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "DEV_ONLY_SIGNING_KEY_MUST_BE_REPLACED_32+",
            ["PasswordReset:DeliveryMode"] = "Logging",
        });

        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment { EnvironmentName = "Staging" };

        var act = () => services.AddInfrastructure(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*development placeholder*");
    }

    private static IConfiguration BuildBaseConfiguration(Dictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "ContentForge",
            ["Jwt:Audience"] = "ContentForge.Admin",
            ["Jwt:SigningKey"] = "PRODUCTION_GRADE_SIGNING_KEY_32CHARS_MIN",
            ["Jwt:AccessTokenLifetimeMinutes"] = "15",
            ["Jwt:RefreshTokenLifetimeDays"] = "7",
            ["Database:Provider"] = "PostgreSQL",
            ["Database:ConnectionString"] = "Host=localhost;Database=x;Username=x;Password=x",
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
