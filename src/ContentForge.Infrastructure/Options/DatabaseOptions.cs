namespace ContentForge.Infrastructure.Options;

/// <summary>
/// Database connection and provider settings.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "PostgreSQL";

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsPostgreSql =>
        Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);

    public bool IsSqlServer =>
        Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
        || Provider.Equals("AzureSQL", StringComparison.OrdinalIgnoreCase);
}
