namespace ContentForge.ArchitectureTests;

using System.Text.RegularExpressions;
using FluentAssertions;

public sealed class MigrationSchemaParityTests
{
    private static readonly string _repositoryRoot = MigrationRepositoryPaths.Root;

    [Fact]
    public void PostgreSQLAndSqlServer_ModelSnapshots_ShouldDefineSameTables()
    {
        var postgresTables = ExtractTableNames(
            Path.Combine(_repositoryRoot, "src/ContentForge.Infrastructure/Migrations/AppDbContextModelSnapshot.cs"));
        var sqlServerTables = ExtractTableNames(
            Path.Combine(_repositoryRoot, "src/ContentForge.Infrastructure.SqlServer/Migrations/AppDbContextModelSnapshot.cs"));

        postgresTables.Should().NotBeEmpty();
        sqlServerTables.Should().NotBeEmpty();
        postgresTables.Should().BeEquivalentTo(sqlServerTables,
            "Azure SQL and PostgreSQL migration snapshots must stay aligned before production deployment");
    }

    private static HashSet<string> ExtractTableNames(string snapshotPath)
    {
        File.Exists(snapshotPath).Should().BeTrue($"snapshot not found: {snapshotPath}");
        var content = File.ReadAllText(snapshotPath);
        var matches = Regex.Matches(content, @"\.ToTable\(""([^""]+)""", RegexOptions.CultureInvariant);

        return matches
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
