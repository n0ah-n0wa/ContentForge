namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Persistence port for content entry aggregates.
/// </summary>
public interface IContentEntryRepository
{
    Task<ContentEntry?> GetByIdAsync(ContentEntryId id, CancellationToken cancellationToken = default);

    Task<ContentEntry?> GetBySlugAsync(ContentTypeId contentTypeId, Slug slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(ContentTypeId contentTypeId, Slug slug, CancellationToken cancellationToken = default);

    Task<PaginatedResult<ContentEntry>> ListAsync(ContentEntryListCriteria criteria, CancellationToken cancellationToken = default);

    Task AddAsync(ContentEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentEntry entry, CancellationToken cancellationToken = default);
}
