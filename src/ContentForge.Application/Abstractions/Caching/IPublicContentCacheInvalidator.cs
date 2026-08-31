namespace ContentForge.Application.Abstractions.Caching;

using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Invalidates cached public content after published representation changes.
/// </summary>
public interface IPublicContentCacheInvalidator
{
    Task InvalidateEntryAsync(
        ContentTypeId contentTypeId,
        Slug entrySlug,
        Slug? publishedSlug = null,
        CancellationToken cancellationToken = default);

    Task InvalidateContentTypeAsync(
        ContentTypeId contentTypeId,
        CancellationToken cancellationToken = default);
}
