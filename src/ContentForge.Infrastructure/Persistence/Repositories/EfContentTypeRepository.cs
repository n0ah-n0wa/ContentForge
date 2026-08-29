namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfContentTypeRepository(AppDbContext dbContext) : IContentTypeRepository
{
    public async Task<ContentType?> GetByIdAsync(ContentTypeId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .SingleOrDefaultAsync(contentType => contentType.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ContentTypeMapper.ToDomain(entity);
    }

    public async Task<ContentType?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .SingleOrDefaultAsync(contentType => contentType.Slug == slug.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ContentTypeMapper.ToDomain(entity);
    }

    public Task<bool> ExistsByNameAsync(FieldName name, CancellationToken cancellationToken = default) =>
        dbContext.ContentTypes.AnyAsync(contentType => contentType.Name == name.Value, cancellationToken);

    public Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default) =>
        dbContext.ContentTypes.AnyAsync(contentType => contentType.Slug == slug.Value, cancellationToken);

    public Task<bool> HasDependentEntriesAsync(ContentTypeId id, CancellationToken cancellationToken = default) =>
        dbContext.ContentEntries.AnyAsync(entry => entry.ContentTypeId == id.Value, cancellationToken);

    public async Task<PaginatedResult<ContentType>> ListAsync(
        ContentTypeListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContentTypes
            .AsNoTracking()
            .Include(contentType => contentType.Fields)
            .AsQueryable();

        if (criteria.IsActive is { } isActive)
        {
            query = query.Where(contentType => contentType.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{PortableSearch.NormalizeTerm(criteria.Search)}%";
            query = query.WhereContentTypeContains(dbContext, pattern);
        }

        query = ApplySort(query, criteria.Sort);

        var page = await query.ToPaginatedResultAsync(criteria.Pagination, cancellationToken).ConfigureAwait(false);
        return new PaginatedResult<ContentType>(
            page.Items.Select(ContentTypeMapper.ToDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task AddAsync(ContentType contentType, CancellationToken cancellationToken = default)
    {
        await dbContext.ContentTypes.AddAsync(ContentTypeMapper.ToEntity(contentType), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ContentType contentType, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        var entity = await dbContext.ContentTypes
            .Include(type => type.Fields)
            .SingleOrDefaultAsync(type => type.Id == contentType.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Content type '{contentType.Id}' was not found.");

        ContentTypeMapper.UpdateEntity(entity, contentType);
        await MarkNewFieldsAsAddedAsync(entity, cancellationToken);
    }

    private async Task MarkNewFieldsAsAddedAsync(
        ContentTypeEntity entity,
        CancellationToken cancellationToken)
    {
        if (entity.Fields.Count == 0)
        {
            return;
        }

        var candidateIds = entity.Fields.Select(field => field.Id).ToList();
        var existingIds = await dbContext.ContentTypeFields
            .AsNoTracking()
            .Where(field => candidateIds.Contains(field.Id))
            .Select(field => field.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingIdSet = existingIds.ToHashSet();
        foreach (var field in entity.Fields)
        {
            if (!existingIdSet.Contains(field.Id))
            {
                dbContext.Entry(field).State = EntityState.Added;
            }
        }
    }

    public async Task DeleteAsync(ContentType contentType, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ContentTypes
            .SingleOrDefaultAsync(type => type.Id == contentType.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            dbContext.ContentTypes.Remove(entity);
        }
    }

    private static IQueryable<ContentTypeEntity> ApplySort(IQueryable<ContentTypeEntity> query, SortRequest sort) =>
        sort.SortBy switch
        {
            "displayName" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(contentType => contentType.DisplayName)
                : query.OrderBy(contentType => contentType.DisplayName),
            "createdAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(contentType => contentType.CreatedAt)
                : query.OrderBy(contentType => contentType.CreatedAt),
            "updatedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(contentType => contentType.UpdatedAt)
                : query.OrderBy(contentType => contentType.UpdatedAt),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(contentType => contentType.Name)
                : query.OrderBy(contentType => contentType.Name),
        };
}
