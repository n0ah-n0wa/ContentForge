namespace ContentForge.Application.Abstractions;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Queries;

/// <summary>
/// Administrative content search port for keyword and field-based queries.
/// </summary>
public interface IContentSearchService
{
    Task<PaginatedResult<Guid>> SearchAsync(
        ContentSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
