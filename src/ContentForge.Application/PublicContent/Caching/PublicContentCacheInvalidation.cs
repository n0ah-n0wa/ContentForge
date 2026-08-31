namespace ContentForge.Application.PublicContent.Caching;

using ContentForge.Application.Abstractions.Caching;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;

internal static class PublicContentCacheInvalidation
{
    internal static Task InvalidateEntryAsync(
        IPublicContentCacheInvalidator invalidator,
        ContentTypeId contentTypeId,
        ContentEntry entry,
        Slug? publishedSlugOverride = null,
        CancellationToken cancellationToken = default)
    {
        var publishedSlug = publishedSlugOverride
            ?? (entry.HasPublishedRepresentation ? entry.PublishedSnapshot!.Slug : null);

        return invalidator.InvalidateEntryAsync(
            contentTypeId,
            entry.Slug,
            publishedSlug,
            cancellationToken);
    }
}
