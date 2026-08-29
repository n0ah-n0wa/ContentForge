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

    Task<PaginatedResult<MediaAsset>> ListAsync(MediaListCriteria criteria, CancellationToken cancellationToken = default);

    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default);
}
