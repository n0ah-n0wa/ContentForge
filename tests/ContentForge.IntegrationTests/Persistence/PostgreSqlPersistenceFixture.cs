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
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await dbContext.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "PostgreSQL test database is unavailable. Start it with: docker compose -f docker-compose.test.yml up -d");
        }

        await dbContext.Database.MigrateAsync();
        await ResetDatabaseAsync();
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
