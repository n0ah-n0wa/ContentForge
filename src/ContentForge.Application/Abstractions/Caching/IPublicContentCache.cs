namespace ContentForge.Application.Abstractions.Caching;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;

/// <summary>
/// Cache for published public content reads. Only stores <see cref="PublicContentDto"/> payloads.
/// </summary>
public interface IPublicContentCache
{
    bool IsEnabled { get; }

    ValueTask<PublicContentDto?> TryGetEntryAsync(
        string contentTypeSlug,
        string slug,
        CancellationToken cancellationToken = default);

    ValueTask SetEntryAsync(
        string contentTypeSlug,
        string slug,
        PublicContentDto entry,
        CancellationToken cancellationToken = default);

    ValueTask<PaginatedResult<PublicContentDto>?> TryGetListAsync(
        string contentTypeSlug,
        string listKey,
        CancellationToken cancellationToken = default);

    ValueTask SetListAsync(
        string contentTypeSlug,
        string listKey,
        PaginatedResult<PublicContentDto> page,
        CancellationToken cancellationToken = default);

    Task InvalidateEntryAsync(
        string contentTypeSlug,
        string slug,
        CancellationToken cancellationToken = default);

    Task InvalidateContentTypeAsync(
        string contentTypeSlug,
        CancellationToken cancellationToken = default);
}
