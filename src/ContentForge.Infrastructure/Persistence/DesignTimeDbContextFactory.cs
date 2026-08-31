namespace ContentForge.Infrastructure.Persistence;

using ContentForge.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../ContentForge.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions
            {
                Provider = "PostgreSQL",
                ConnectionString = "Host=localhost;Port=5432;Database=contentforge;Username=contentforge;Password=contentforge",
            };

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureProvider(optionsBuilder, databaseOptions);
        return new AppDbContext(optionsBuilder.Options);
    }

    internal static void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, DatabaseOptions databaseOptions)
    {
        if (databaseOptions.IsPostgreSql)
        {
            optionsBuilder.UseNpgsql(
                databaseOptions.ConnectionString,
                builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            return;
        }

        if (databaseOptions.IsSqlServer)
        {
            optionsBuilder.UseSqlServer(
                databaseOptions.ConnectionString,
                builder => builder.MigrationsAssembly("ContentForge.Infrastructure.SqlServer"));
            return;
        }

        throw new InvalidOperationException($"Unsupported database provider '{databaseOptions.Provider}'.");
    }
}
