namespace ContentForge.Api.Infrastructure;

using ContentForge.Application.Common.Filtering;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;

/// <summary>
/// Parses whitelisted query-string parameters into primitive values.
/// </summary>
internal static class QueryBinding
{
    private static readonly string[] _pagingAndSortParameters =
    [
        "page",
        "pageSize",
        "sortBy",
        "sortDirection",
    ];

    internal static PaginationRequest GetPagination(int page, int pageSize) =>
        new(page, pageSize);

    internal static SortRequest CreateSort(string? sortBy, string? sortDirection, string defaultSortBy, SortDirection defaultDirection = SortDirection.Asc)
    {
        var direction = defaultDirection;
        if (!string.IsNullOrWhiteSpace(sortDirection))
        {
            if (!Enum.TryParse<SortDirection>(sortDirection, ignoreCase: true, out direction))
            {
                throw new UnsupportedQueryParameterException(
                    "sortDirection",
                    $"Sort direction '{sortDirection}' is not supported. Allowed values: asc, desc.");
            }
        }

        return new SortRequest(
            string.IsNullOrWhiteSpace(sortBy) ? defaultSortBy : sortBy,
            direction);
    }

    internal static TEnum? ParseOptionalEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new UnsupportedQueryParameterException(
                parameterName,
                $"Value '{value}' is not supported for '{parameterName}'.");
        }

        return parsed;
    }

    internal static void EnsureAllowedQueryParameters(IQueryCollection query, params string[] allowedNames)
    {
        var allowed = new HashSet<string>(allowedNames, StringComparer.OrdinalIgnoreCase);
        allowed.UnionWith(_pagingAndSortParameters);

        foreach (var key in query.Keys)
        {
            if (!allowed.Contains(key))
            {
                throw new UnsupportedQueryParameterException(
                    key,
                    $"Query parameter '{key}' is not supported. Allowed parameters: {string.Join(", ", allowed.Order(StringComparer.Ordinal))}.");
            }
        }
    }

    internal static IReadOnlyDictionary<string, string?> CollectUnrecognizedParameters(
        IQueryCollection query,
        params string[] reservedNames)
    {
        var reserved = new HashSet<string>(reservedNames, StringComparer.OrdinalIgnoreCase);
        reserved.UnionWith(_pagingAndSortParameters);

        var unrecognized = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var key in query.Keys)
        {
            if (!reserved.Contains(key))
            {
                unrecognized[key] = query[key].ToString();
            }
        }

        return unrecognized;
    }
}
