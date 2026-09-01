namespace ContentForge.ArchitectureTests;

using System.Text.RegularExpressions;
using FluentAssertions;

public sealed class MigrationSafetyTests
{
    private static readonly string _repositoryRoot = MigrationRepositoryPaths.Root;

    private static readonly (Regex Pattern, string Label)[] _destructiveUpPatterns =
    [
        (new Regex(@"migrationBuilder\.DropTable\s*\(", RegexOptions.CultureInvariant), "DropTable"),
        (new Regex(@"migrationBuilder\.DropColumn\s*\(", RegexOptions.CultureInvariant), "DropColumn"),
        (new Regex(@"migrationBuilder\.DeleteData\s*\(", RegexOptions.CultureInvariant), "DeleteData"),
        (new Regex(@"migrationBuilder\.DropDatabase\s*\(", RegexOptions.CultureInvariant), "DropDatabase"),
        (new Regex(@"EnsureDeleted\s*\(", RegexOptions.CultureInvariant), "EnsureDeleted"),
        (new Regex(@"DROP\s+DATABASE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "DROP DATABASE SQL"),
        (new Regex(@"TRUNCATE\s+TABLE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "TRUNCATE TABLE SQL"),
    ];

    [Fact]
    public void MigrationUpMethods_ShouldNotContainUnapprovedDestructiveOperations()
    {
        var allowlist = LoadAllowlist();
        var violations = new List<string>();

        foreach (var migrationFile in EnumerateMigrationFiles())
        {
            var upBody = ExtractUpMethodBody(migrationFile);
            if (upBody is null)
            {
                continue;
            }

            foreach (var (pattern, label) in _destructiveUpPatterns)
            {
                if (!pattern.IsMatch(upBody) || allowlist.Contains(migrationFile.Name))
                {
                    continue;
                }

                violations.Add($"{migrationFile.FullName}: unapproved {label} in Up()");
            }
        }

        violations.Should().BeEmpty(
            "destructive Up() operations require an entry in infra/azure/scripts/destructive-migration-allowlist.txt");
    }

    [Fact]
    public void MigrationDownMethods_ShouldNotDropDatabase()
    {
        var forbidden = new[]
        {
            new Regex(@"migrationBuilder\.DropDatabase\s*\(", RegexOptions.CultureInvariant),
            new Regex(@"DROP\s+DATABASE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        };

        var violations = new List<string>();
        foreach (var migrationFile in EnumerateMigrationFiles())
        {
            var downBody = ExtractDownMethodBody(migrationFile);
            if (downBody is null)
            {
                continue;
            }

            foreach (var pattern in forbidden)
            {
                if (pattern.IsMatch(downBody))
                {
                    violations.Add($"{migrationFile.FullName}: forbidden {pattern} in Down()");
                }
            }
        }

        violations.Should().BeEmpty("database drop operations are never permitted in migrations");
    }

    [Fact]
    public void ProductionInitializer_ShouldNotAutoMigrateOutsideDevelopment()
    {
        var path = Path.Combine(
            _repositoryRoot,
            "src/ContentForge.Infrastructure/Persistence/Development/DevelopmentDatabaseInitializer.cs");
        var content = File.ReadAllText(path);

        content.Should().Contain("IsDevelopment()", "migrations must not auto-apply outside Development");
        content.Should().NotContain("IsProduction()", "production must not trigger startup migrations");
        content.Should().NotContain("IsStaging()", "staging must not trigger startup migrations");
    }

    private static IEnumerable<FileInfo> EnumerateMigrationFiles()
    {
        var directories = new[]
        {
            Path.Combine(_repositoryRoot, "src/ContentForge.Infrastructure/Migrations"),
            Path.Combine(_repositoryRoot, "src/ContentForge.Infrastructure.SqlServer/Migrations"),
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
            {
                if (file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("AppDbContextModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new FileInfo(file);
            }
        }
    }

    private static HashSet<string> LoadAllowlist()
    {
        var allowlistPath = Path.Combine(
            _repositoryRoot,
            "infra/azure/scripts/destructive-migration-allowlist.txt");

        if (!File.Exists(allowlistPath))
        {
            return [];
        }

        return File.ReadAllLines(allowlistPath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
            .Select(line => line.Split(':', 2)[0].Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ExtractUpMethodBody(FileInfo migrationFile)
    {
        var content = File.ReadAllText(migrationFile.FullName);
        var match = Regex.Match(
            content,
            @"protected\s+override\s+void\s+Up\s*\(\s*MigrationBuilder\s+\w+\s*\)(.*?)(?=protected\s+override\s+void\s+Down|\Z)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractDownMethodBody(FileInfo migrationFile)
    {
        var content = File.ReadAllText(migrationFile.FullName);
        var match = Regex.Match(
            content,
            @"protected\s+override\s+void\s+Down\s*\(\s*MigrationBuilder\s+\w+\s*\)(.*?)(?=^\s*}\s*$|\Z)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups[1].Value : null;
    }
}
