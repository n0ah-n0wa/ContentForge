namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Media.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfMediaRepository(AppDbContext dbContext) : IMediaRepository
{
    public async Task<MediaAsset?> GetByIdAsync(MediaId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MediaAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(media => media.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : MediaAssetMapper.ToDomain(entity);
    }

    public async Task<PaginatedResult<MediaAsset>> ListAsync(
        MediaListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MediaAssets.AsNoTracking();

        if (!criteria.IncludeDeleted)
        {
            query = query.Where(media => !media.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            query = query.Where(media => media.ContentType == criteria.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{PortableSearch.NormalizeTerm(criteria.Search)}%";
            query = query.WhereMediaContains(dbContext, pattern);
        }

        query = ApplySort(query, criteria.Sort);

        var page = await query.ToPaginatedResultAsync(criteria.Pagination, cancellationToken).ConfigureAwait(false);
        return new PaginatedResult<MediaAsset>(
            page.Items.Select(MediaAssetMapper.ToDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        await dbContext.MediaAssets.AddAsync(MediaAssetMapper.ToEntity(asset), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.MediaAssets
            .SingleOrDefaultAsync(media => media.Id == asset.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Media asset '{asset.Id}' was not found.");

        MediaAssetMapper.UpdateEntity(entity, asset);
    }

    private static IQueryable<MediaAssetEntity> ApplySort(IQueryable<MediaAssetEntity> query, SortRequest sort) =>
        sort.SortBy switch
        {
            "uploadedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(media => media.UploadedAt)
                : query.OrderBy(media => media.UploadedAt),
            "size" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(media => media.Size)
                : query.OrderBy(media => media.Size),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(media => media.FileName)
                : query.OrderBy(media => media.FileName),
        };
}
