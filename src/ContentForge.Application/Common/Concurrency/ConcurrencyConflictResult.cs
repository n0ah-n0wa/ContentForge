namespace ContentForge.Application.Common.Concurrency;

/// <summary>
/// Application-level conflict payload for stale optimistic concurrency tokens.
/// </summary>
public sealed record ConcurrencyConflictResult(
    uint ExpectedVersion,
    uint ActualVersion,
    DateTimeOffset UpdatedAt);
