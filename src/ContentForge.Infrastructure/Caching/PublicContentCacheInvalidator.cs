namespace ContentForge.Infrastructure.Caching;

using ContentForge.Application.Abstractions.Caching;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;

internal sealed class PublicContentCacheInvalidator(
    IContentTypeRepository contentTypeRepository,
    IPublicContentCache cache) : IPublicContentCacheInvalidator
{
    public async Task InvalidateEntryAsync(
        ContentTypeId contentTypeId,
        Slug entrySlug,
        Slug? publishedSlug = null,
        CancellationToken cancellationToken = default)
    {
        if (!cache.IsEnabled)
        {
            return;
        }

        var contentType = await contentTypeRepository.GetByIdAsync(contentTypeId, cancellationToken).ConfigureAwait(false);
        if (contentType is null)
        {
            return;
        }

        var contentTypeSlug = contentType.Slug.Value;
        await cache.InvalidateEntryAsync(contentTypeSlug, entrySlug.Value, cancellationToken).ConfigureAwait(false);

        if (publishedSlug is not null && publishedSlug.Value != entrySlug.Value)
        {
            await cache.InvalidateEntryAsync(contentTypeSlug, publishedSlug.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task InvalidateContentTypeAsync(
        ContentTypeId contentTypeId,
        CancellationToken cancellationToken = default)
    {
        if (!cache.IsEnabled)
        {
            return;
        }

        var contentType = await contentTypeRepository.GetByIdAsync(contentTypeId, cancellationToken).ConfigureAwait(false);
        if (contentType is null)
        {
            return;
        }

        await cache.InvalidateContentTypeAsync(contentType.Slug.Value, cancellationToken).ConfigureAwait(false);
    }
}
