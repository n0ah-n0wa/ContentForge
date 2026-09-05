namespace ContentForge.Application.Abstractions.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Media.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;

/// <summary>
/// Persistence port for media asset metadata.
/// </summary>
public interface IMediaRepository
{
    Task<MediaAsset?> GetByIdAsync(MediaId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when an active (non-deleted) media asset exists for the storage key.
    /// Used to gate anonymous binary delivery after soft-delete.
    /// </summary>
    Task<bool> ExistsActiveByStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<PaginatedResult<MediaAsset>> ListAsync(MediaListCriteria criteria, CancellationToken cancellationToken = default);

    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> FindUnavailableIdsAsync(
        IReadOnlyCollection<Guid> mediaIds,
        CancellationToken cancellationToken = default);
}
