namespace ContentForge.Application.Common.Pagination;

/// <summary>
/// Server-side pagination limits enforced by the application layer.
/// </summary>
public static class PaginationDefaults
{
    public const int MinPage = 1;
    public const int DefaultPage = 1;
    public const int MinPageSize = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
