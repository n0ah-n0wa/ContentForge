namespace ContentForge.Application.Common.Sorting;

/// <summary>
/// Sorting contract that only permits explicitly whitelisted fields.
/// </summary>
public sealed record SortRequest(string SortBy, SortDirection Direction = SortDirection.Asc)
{
    public static SortRequest Default(string defaultSortBy) => new(defaultSortBy, SortDirection.Asc);

    public void EnsureAllowed(IReadOnlySet<string> allowedFields, string resourceName)
    {
        if (!allowedFields.Contains(SortBy))
        {
            throw new Filtering.UnsupportedQueryParameterException(
                nameof(SortBy),
                $"Sorting by '{SortBy}' is not supported for {resourceName}. Allowed fields: {string.Join(", ", allowedFields)}.");
        }
    }
}
