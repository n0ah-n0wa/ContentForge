using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContentForge.IntegrationTests;

public sealed class ContentForgeWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CONTENTFORGE_TEST_DB_CONNECTION")
                ?? "Host=localhost;Port=5433;Database=contentforge_test;Username=contentforge;Password=contentforge";

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSQL",
                ["Database:ConnectionString"] = connectionString,
                ["Jwt:Issuer"] = "ContentForge.Test",
                ["Jwt:Audience"] = "ContentForge.Test.Admin",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_32_CHARS_MINIMUM_VALUE",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
            });
        });
    }
}
