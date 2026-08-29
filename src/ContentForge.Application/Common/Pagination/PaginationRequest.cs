namespace ContentForge.Application.Common.Pagination;

/// <summary>
/// Standard pagination request contract for collection queries.
/// </summary>
public sealed record PaginationRequest(int Page = PaginationDefaults.DefaultPage, int PageSize = PaginationDefaults.DefaultPageSize)
{
    public int NormalizedPage => Page < PaginationDefaults.MinPage ? PaginationDefaults.MinPage : Page;

    public int NormalizedPageSize
    {
        get
        {
            if (PageSize < PaginationDefaults.MinPageSize)
            {
                return PaginationDefaults.MinPageSize;
            }

            return PageSize > PaginationDefaults.MaxPageSize
                ? PaginationDefaults.MaxPageSize
                : PageSize;
        }
    }

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
