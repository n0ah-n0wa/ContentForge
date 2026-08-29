namespace ContentForge.Infrastructure.Persistence;

/// <summary>
/// Provider-specific index filter expressions for EF Core filtered indexes.
/// </summary>
internal static class DatabaseIndexFilters
{
    internal const string PostgreSqlSoftDelete = "\"IsDeleted\" = false";

    internal const string SqlServerSoftDelete = "[IsDeleted] = 0";

    internal static string SoftDelete(bool isPostgreSql) =>
        isPostgreSql ? PostgreSqlSoftDelete : SqlServerSoftDelete;
}
