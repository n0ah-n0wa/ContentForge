namespace ContentForge.Application.Common.Filtering;

/// <summary>
/// Validates that only whitelisted filter parameters are supplied.
/// </summary>
public static class FilterValidator
{
    public static void EnsureAllowed(
        IReadOnlyDictionary<string, string?> suppliedFilters,
        IReadOnlySet<string> allowedFilters,
        string resourceName)
    {
        foreach (var filterName in suppliedFilters.Keys)
        {
            if (!allowedFilters.Contains(filterName))
            {
                throw new UnsupportedQueryParameterException(
                    filterName,
                    $"Filter '{filterName}' is not supported for {resourceName}. Allowed filters: {string.Join(", ", allowedFilters)}.");
            }
        }
    }
}
