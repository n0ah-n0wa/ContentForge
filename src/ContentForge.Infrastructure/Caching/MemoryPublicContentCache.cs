namespace ContentForge.Infrastructure.Caching;

using System.Collections.Concurrent;
using ContentForge.Application.Abstractions.Caching;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Application.PublicContent.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

internal sealed class MemoryPublicContentCache : IPublicContentCache, IPublicContentCacheStatistics
{
    private readonly IMemoryCache _memoryCache;
    private readonly PublicContentCacheOptions _options;
    private readonly ConcurrentDictionary<string, long> _listVersions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _contentTypeVersions = new(StringComparer.Ordinal);
    private long _entryHits;
    private long _entryMisses;
    private long _listHits;
    private long _listMisses;

    public MemoryPublicContentCache(IMemoryCache memoryCache, IOptions<PublicContentCacheOptions> options)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public bool IsEnabled => _options.Enabled;

    public long EntryHits => Interlocked.Read(ref _entryHits);

    public long EntryMisses => Interlocked.Read(ref _entryMisses);

    public long ListHits => Interlocked.Read(ref _listHits);

    public long ListMisses => Interlocked.Read(ref _listMisses);

    public void Reset()
    {
        Interlocked.Exchange(ref _entryHits, 0);
        Interlocked.Exchange(ref _entryMisses, 0);
        Interlocked.Exchange(ref _listHits, 0);
        Interlocked.Exchange(ref _listMisses, 0);
    }

    public ValueTask<PublicContentDto?> TryGetEntryAsync(
        string contentTypeSlug,
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Interlocked.Increment(ref _entryMisses);
            return ValueTask.FromResult<PublicContentDto?>(null);
        }

        if (_memoryCache.TryGetValue(EntryCacheKey(contentTypeSlug, slug), out PublicContentDto? cached))
        {
            Interlocked.Increment(ref _entryHits);
            return ValueTask.FromResult(cached);
        }

        Interlocked.Increment(ref _entryMisses);
        return ValueTask.FromResult<PublicContentDto?>(null);
    }

    public ValueTask SetEntryAsync(
        string contentTypeSlug,
        string slug,
        PublicContentDto entry,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return ValueTask.CompletedTask;
        }

        _memoryCache.Set(
            EntryCacheKey(contentTypeSlug, slug),
            entry,
            CreateEntryOptions(_options.EntryTtlSeconds));

        return ValueTask.CompletedTask;
    }

    public ValueTask<PaginatedResult<PublicContentDto>?> TryGetListAsync(
        string contentTypeSlug,
        string listKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            Interlocked.Increment(ref _listMisses);
            return ValueTask.FromResult<PaginatedResult<PublicContentDto>?>(null);
        }

        if (_memoryCache.TryGetValue(ListCacheKey(contentTypeSlug, listKey), out PaginatedResult<PublicContentDto>? cached))
        {
            Interlocked.Increment(ref _listHits);
            return ValueTask.FromResult(cached);
        }

        Interlocked.Increment(ref _listMisses);
        return ValueTask.FromResult<PaginatedResult<PublicContentDto>?>(null);
    }

    public ValueTask SetListAsync(
        string contentTypeSlug,
        string listKey,
        PaginatedResult<PublicContentDto> page,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return ValueTask.CompletedTask;
        }

        _memoryCache.Set(
            ListCacheKey(contentTypeSlug, listKey),
            page,
            CreateEntryOptions(_options.ListTtlSeconds));

        return ValueTask.CompletedTask;
    }

    public Task InvalidateEntryAsync(string contentTypeSlug, string slug, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        _memoryCache.Remove(EntryCacheKey(contentTypeSlug, slug));
        BumpListVersion(contentTypeSlug);
        return Task.CompletedTask;
    }

    public Task InvalidateContentTypeAsync(string contentTypeSlug, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        BumpContentTypeVersion(contentTypeSlug);
        BumpListVersion(contentTypeSlug);
        return Task.CompletedTask;
    }

    private static MemoryCacheEntryOptions CreateEntryOptions(int ttlSeconds) =>
        new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(Math.Max(1, ttlSeconds)))
            .SetSize(1);

    private long GetListVersion(string contentTypeSlug) =>
        _listVersions.GetOrAdd(NormalizeSlug(contentTypeSlug), static _ => 0);

    private long GetContentTypeVersion(string contentTypeSlug) =>
        _contentTypeVersions.GetOrAdd(NormalizeSlug(contentTypeSlug), static _ => 0);

    private void BumpListVersion(string contentTypeSlug) =>
        _listVersions.AddOrUpdate(
            NormalizeSlug(contentTypeSlug),
            static _ => 1,
            static (_, version) => version + 1);

    private void BumpContentTypeVersion(string contentTypeSlug) =>
        _contentTypeVersions.AddOrUpdate(
            NormalizeSlug(contentTypeSlug),
            static _ => 1,
            static (_, version) => version + 1);

    private string EntryCacheKey(string contentTypeSlug, string slug) =>
        $"public:entry:{NormalizeSlug(contentTypeSlug)}:v{GetContentTypeVersion(contentTypeSlug)}:{NormalizeSlug(slug)}";

    private string ListCacheKey(string contentTypeSlug, string listKey) =>
        $"public:list:{NormalizeSlug(contentTypeSlug)}:v{GetListVersion(contentTypeSlug)}:{listKey}";

    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
}
