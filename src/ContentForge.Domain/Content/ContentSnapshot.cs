namespace ContentForge.Domain.Content;

/// <summary>
/// Captures the state of a content entry at a point in time.
/// </summary>
public sealed record ContentSnapshot(
    Common.Slug Slug,
    ContentData Data,
    ContentStatus Status)
{
    public static ContentSnapshot FromEntry(ContentEntry entry) =>
        new(entry.Slug, entry.DraftData.Clone(), entry.Status);
}
