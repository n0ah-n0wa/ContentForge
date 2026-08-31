using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ContentForge.IntegrationTests;

public sealed class ContentForgeWebApplicationFactory : WebApplicationFactory<Program>
{
    public string MediaRoot { get; } = Path.Combine(
        Path.GetTempPath(),
        "contentforge-media-tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(MediaRoot);

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
                ["Media:Provider"] = "Local",
                ["Media:LocalRoot"] = MediaRoot,
                ["Media:PublicBaseUrl"] = "/media-files",
                ["ScheduledPublishing:PollIntervalSeconds"] = "1",
                ["ScheduledPublishing:BatchSize"] = "20",
                ["ScheduledPublishing:LockDurationSeconds"] = "30",
                ["ScheduledPublishing:MaxAttempts"] = "5",
                ["ScheduledPublishing:RetryDelaySeconds"] = "5",
                ["ContentPreview:TokenLifetimeMinutes"] = "15",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(MediaRoot))
        {
            try
            {
                Directory.Delete(MediaRoot, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup for temporary media files.
            }
        }
    }
}
