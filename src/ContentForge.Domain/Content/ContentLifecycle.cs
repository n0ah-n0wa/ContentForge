namespace ContentForge.Domain.Content;

/// <summary>
/// Encapsulates content lifecycle transition rules.
/// </summary>
public static class ContentLifecycle
{
    private static readonly Dictionary<ContentStatus, HashSet<ContentStatus>> _allowedTransitions =
        new()
        {
            [ContentStatus.Draft] = [ContentStatus.InReview],
            [ContentStatus.InReview] = [ContentStatus.Draft, ContentStatus.Published],
            [ContentStatus.Published] = [ContentStatus.Draft, ContentStatus.Archived, ContentStatus.Unpublished],
            [ContentStatus.Unpublished] = [ContentStatus.Draft],
            [ContentStatus.Archived] = [ContentStatus.Draft],
        };

    public static bool CanTransition(ContentStatus current, ContentStatus target) =>
        _allowedTransitions.TryGetValue(current, out var targets) && targets.Contains(target);

    public static void EnsureTransition(ContentStatus current, ContentStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new Common.InvalidOperationDomainException(
                $"Invalid content status transition from '{current}' to '{target}'.");
        }
    }
}
