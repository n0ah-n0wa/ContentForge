namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Persistence port for content type aggregates.
/// </summary>
public interface IContentTypeRepository
{
    Task<ContentType?> GetByIdAsync(ContentTypeId id, CancellationToken cancellationToken = default);

    Task<ContentType?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(FieldName name, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<bool> HasDependentEntriesAsync(ContentTypeId id, CancellationToken cancellationToken = default);

    Task<PaginatedResult<ContentType>> ListAsync(ContentTypeListCriteria criteria, CancellationToken cancellationToken = default);

    Task AddAsync(ContentType contentType, CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentType contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(ContentType contentType, CancellationToken cancellationToken = default);
}
