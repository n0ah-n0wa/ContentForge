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

    public async Task<ContentEntry?> GetPublishedBySlugAsync(
        ContentTypeId contentTypeId,
        Slug slug,
        CancellationToken cancellationToken = default)
    {
        var slugValue = slug.Value;

        var entity = await dbContext.ContentEntries
            .AsNoTracking()
            .Where(entry =>
                entry.ContentTypeId == contentTypeId.Value
                && !entry.IsDeleted
                && entry.PublishedSnapshotJson != null
                && entry.PublishedAt != null
                && entry.Slug == slugValue)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            var matched = ContentEntryMapper.ToDomainPublicSummary(entity);
            if (matched.HasPublishedRepresentation && matched.PublishedSnapshot!.Slug.Value == slugValue)
            {
                return matched;
            }
        }

        var snapshotMarker = $"\"slug\":\"{slugValue}\"";
        var fallbackMatches = await dbContext.ContentEntries
            .AsNoTracking()
            .Where(entry =>
                entry.ContentTypeId == contentTypeId.Value
                && !entry.IsDeleted
                && entry.PublishedSnapshotJson != null
                && entry.PublishedAt != null
                && entry.Slug != slugValue
                && entry.PublishedSnapshotJson.Contains(snapshotMarker))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return fallbackMatches
            .Select(ContentEntryMapper.ToDomainPublicSummary)
            .SingleOrDefault(entry =>
                entry.HasPublishedRepresentation
                && entry.PublishedSnapshot!.Slug.Value == slugValue);
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

        if (criteria.PublishedRepresentationOnly)
        {
            query = query.Where(entry => entry.PublishedSnapshotJson != null && entry.PublishedAt != null);
        }

        if (criteria.AuthorId is { } authorId)
        {
            query = query.Where(entry => entry.CreatedBy == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = PortableSearch.CreateContainsPattern(criteria.Search);
            query = query.WhereSlugContains(dbContext, pattern);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ExactSlug))
        {
            var exactSlug = criteria.ExactSlug;
            if (criteria.PublishedRepresentationOnly)
            {
                var snapshotMarker = $"\"slug\":\"{exactSlug}\"";
                query = query.Where(entry =>
                    entry.Slug == exactSlug
                    || (entry.PublishedSnapshotJson != null && entry.PublishedSnapshotJson.Contains(snapshotMarker)));
            }
            else
            {
                query = query.Where(entry => entry.Slug == exactSlug);
            }
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
            page.Items.Select(entity => MapListEntity(entity, criteria.PublishedRepresentationOnly)).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<IReadOnlyList<ContentEntry>> GetSummariesByIdsAsync(
        IReadOnlyList<ContentEntryId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idValues = ids.Select(id => id.Value).ToList();
        var entities = await dbContext.ContentEntries
            .AsNoTracking()
            .Where(entry => idValues.Contains(entry.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entitiesById = entities.ToDictionary(entry => entry.Id);
        var summaries = new List<ContentEntry>(ids.Count);

        foreach (var id in ids)
        {
            if (entitiesById.TryGetValue(id.Value, out var entity))
            {
                summaries.Add(ContentEntryMapper.ToDomainSummary(entity));
            }
        }

        return summaries;
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

        var persistedToken = checked((uint)entity.ConcurrencyToken);
        var expectedOriginal = entry.ConcurrencyToken.Previous().Value;
        if (persistedToken != expectedOriginal)
        {
            throw new ConcurrencyConflictException(expectedOriginal, persistedToken);
        }

        var originalToken = entity.ConcurrencyToken;
        ContentEntryMapper.UpdateEntity(entity, entry);
        dbContext.Entry(entity).Property(contentEntry => contentEntry.ConcurrencyToken).OriginalValue = originalToken;
        await MarkNewVersionsAsAddedAsync(entity, cancellationToken);
    }

    public async Task DeleteAllByContentTypeIdAsync(
        ContentTypeId contentTypeId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.ContentEntries
            .Where(entry => entry.ContentTypeId == contentTypeId.Value)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static ContentEntry MapListEntity(ContentEntryEntity entity, bool publishedRepresentationOnly) =>
        publishedRepresentationOnly
            ? ContentEntryMapper.ToDomainPublicSummary(entity)
            : ContentEntryMapper.ToDomainSummary(entity);

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
