namespace ContentForge.Infrastructure.Persistence;

/// <summary>
/// Normalizes user-provided search terms and builds safe SQL LIKE/ILIKE patterns.
/// </summary>
internal static class PortableSearch
{
    internal const string LikeEscapeCharacter = "\\";

    internal static string NormalizeTerm(string value) => value.Trim();

    internal static string CreateContainsPattern(string value)
    {
        var normalized = EscapeLikePattern(NormalizeTerm(value));
        return $"%{normalized}%";
    }

    internal static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
