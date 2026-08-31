namespace ContentForge.Application.Abstractions.Caching;

/// <summary>
/// Exposes cache hit/miss counters for diagnostics and tests.
/// </summary>
public interface IPublicContentCacheStatistics
{
    long EntryHits { get; }

    long EntryMisses { get; }

    long ListHits { get; }

    long ListMisses { get; }

    void Reset();
}
