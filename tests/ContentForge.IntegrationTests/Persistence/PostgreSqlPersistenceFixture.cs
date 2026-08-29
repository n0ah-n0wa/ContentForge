namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Infrastructure;
using ContentForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class PostgreSqlPersistenceFixture : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;

    internal IServiceProvider Services =>
        _serviceProvider ?? throw new InvalidOperationException("Persistence fixture has not been initialized.");

    internal string ConnectionString { get; private set; } = PersistenceTestConstants.DefaultConnectionString;

    public async Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable("CONTENTFORGE_TEST_DB_CONNECTION")
            ?? PersistenceTestConstants.DefaultConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSQL",
                ["Database:ConnectionString"] = ConnectionString,
                ["Jwt:Issuer"] = "ContentForge.Test",
                ["Jwt:Audience"] = "ContentForge.Test.Admin",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_32_CHARS_MINIMUM_VALUE",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await WaitForDatabaseAsync(dbContext);

        await dbContext.Database.MigrateAsync();
        await ResetDatabaseAsync();
    }

    private static async Task WaitForDatabaseAsync(AppDbContext dbContext)
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException(
            "PostgreSQL test database is unavailable. Start it with: docker compose -f docker-compose.test.yml up -d");
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    internal AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    internal async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "RefreshTokens",
                "UserClaims",
                "UserLogins",
                "UserTokens",
                "ContentEntryRelations",
                "ContentVersions",
                "ContentEntries",
                "ContentTypeFields",
                "ContentTypes",
                "Media",
                "AuditLogs",
                "UserRoles",
                "Users"
            RESTART IDENTITY CASCADE;
            """);
    }
}
