namespace ContentForge.Application.PublicContent.Caching;

/// <summary>
/// Configuration for public content read caching.
/// </summary>
public sealed class PublicContentCacheOptions
{
    public const string SectionName = "PublicContentCache";

    public bool Enabled { get; init; } = true;

    public int EntryTtlSeconds { get; init; } = 300;

    public int ListTtlSeconds { get; init; } = 60;

    public int MaxEntries { get; init; } = 1024;
}
