namespace ContentForge.Infrastructure.Persistence.Repositories;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

internal sealed class EfContentEntryRepository(AppDbContext dbContext) : IContentEntryRepository
{
    public async Task<ContentEntry?> GetByIdAsync(ContentEntryId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ContentEntries
            .AsNoTracking()
            .Include(entry => entry.Versions)
            .SingleOrDefaultAsync(entry => entry.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ContentEntryMapper.ToDomain(entity);
    }

    public async Task<ContentEntry?> GetBySlugAsync(
        ContentTypeId contentTypeId,
        Slug slug,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ContentEntries
            .AsNoTracking()
            .Include(entry => entry.Versions)
            .SingleOrDefaultAsync(
                entry => entry.ContentTypeId == contentTypeId.Value
                    && entry.Slug == slug.Value
                    && !entry.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ContentEntryMapper.ToDomain(entity);
    }

    public Task<bool> ExistsBySlugAsync(
        ContentTypeId contentTypeId,
        Slug slug,
        CancellationToken cancellationToken = default) =>
        dbContext.ContentEntries.AnyAsync(
            entry => entry.ContentTypeId == contentTypeId.Value
                && entry.Slug == slug.Value
                && !entry.IsDeleted,
            cancellationToken);

    public async Task<PaginatedResult<ContentEntry>> ListAsync(
        ContentEntryListCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContentEntries
            .AsNoTracking()
            .AsQueryable();

        if (!criteria.IncludeDeleted)
        {
            query = query.Where(entry => !entry.IsDeleted);
        }

        if (criteria.ContentTypeId is { } contentTypeId)
        {
            query = query.Where(entry => entry.ContentTypeId == contentTypeId.Value);
        }

        if (criteria.Status is { } status)
        {
            var statusValue = status.ToString();
            query = query.Where(entry => entry.Status == statusValue);
        }

        if (criteria.AuthorId is { } authorId)
        {
            query = query.Where(entry => entry.CreatedBy == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{PortableSearch.NormalizeTerm(criteria.Search)}%";
            query = query.WhereSlugContains(dbContext, pattern);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ExactSlug))
        {
            query = query.Where(entry => entry.Slug == criteria.ExactSlug);
        }

        if (criteria.PublishedFrom is { } publishedFrom)
        {
            query = query.Where(entry => entry.PublishedAt != null && entry.PublishedAt >= publishedFrom);
        }

        if (criteria.PublishedTo is { } publishedTo)
        {
            query = query.Where(entry => entry.PublishedAt != null && entry.PublishedAt <= publishedTo);
        }

        query = ApplySort(query, criteria.Sort);

        var page = await query.ToPaginatedResultAsync(criteria.Pagination, cancellationToken).ConfigureAwait(false);
        return new PaginatedResult<ContentEntry>(
            page.Items.Select(ContentEntryMapper.ToDomainSummary).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task AddAsync(ContentEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.ContentEntries.AddAsync(ContentEntryMapper.ToEntity(entry), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ContentEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        var entity = await dbContext.ContentEntries
            .Include(contentEntry => contentEntry.Versions)
            .SingleOrDefaultAsync(contentEntry => contentEntry.Id == entry.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Content entry '{entry.Id}' was not found.");

        ContentEntryMapper.UpdateEntity(entity, entry);
        await MarkNewVersionsAsAddedAsync(entity, cancellationToken);
    }

    public async Task DeleteAllByContentTypeIdAsync(
        ContentTypeId contentTypeId,
        CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.ContentEntries
            .Where(entry => entry.ContentTypeId == contentTypeId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entries.Count == 0)
        {
            return;
        }

        dbContext.ContentEntries.RemoveRange(entries);
    }

    private async Task MarkNewVersionsAsAddedAsync(
        ContentEntryEntity entity,
        CancellationToken cancellationToken)
    {
        if (entity.Versions.Count == 0)
        {
            return;
        }

        var candidateIds = entity.Versions.Select(version => version.Id).ToList();
        var existingIds = await dbContext.ContentVersions
            .AsNoTracking()
            .Where(version => candidateIds.Contains(version.Id))
            .Select(version => version.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingIdSet = existingIds.ToHashSet();
        foreach (var version in entity.Versions)
        {
            if (!existingIdSet.Contains(version.Id))
            {
                dbContext.Entry(version).State = EntityState.Added;
            }
        }
    }

    private static IQueryable<ContentEntryEntity> ApplySort(IQueryable<ContentEntryEntity> query, SortRequest sort) =>
        sort.SortBy switch
        {
            "status" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.Status)
                : query.OrderBy(entry => entry.Status),
            "createdAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.CreatedAt)
                : query.OrderBy(entry => entry.CreatedAt),
            "updatedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.UpdatedAt)
                : query.OrderBy(entry => entry.UpdatedAt),
            "publishedAt" => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.PublishedAt)
                : query.OrderBy(entry => entry.PublishedAt),
            _ => sort.Direction == SortDirection.Desc
                ? query.OrderByDescending(entry => entry.Slug)
                : query.OrderBy(entry => entry.Slug),
        };
}
