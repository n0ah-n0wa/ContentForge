namespace ContentForge.Api.Contracts;

/// <summary>
/// Request body for creating a content entry.
/// </summary>
public sealed record CreateContentApiRequest(
    Guid ContentTypeId,
    string Slug,
    IReadOnlyDictionary<string, object?>? Data);

/// <summary>
/// Request body for updating a content entry.
/// </summary>
public sealed record UpdateContentApiRequest(
    string Slug,
    IReadOnlyDictionary<string, object?> Data,
    string ChangeSummary,
    uint ConcurrencyToken);

/// <summary>
/// Request body for deleting a content entry.
/// </summary>
public sealed record DeleteContentApiRequest(uint ConcurrencyToken);

/// <summary>
/// Request body for lifecycle transitions that require a change summary.
/// </summary>
public sealed record ContentLifecycleApiRequest(
    string ChangeSummary,
    uint ConcurrencyToken);

/// <summary>
/// Request body for lifecycle transitions that only require concurrency.
/// </summary>
public sealed record ContentConcurrencyApiRequest(uint ConcurrencyToken);

/// <summary>
/// Request body for restoring a historical content version.
/// </summary>
public sealed record RestoreContentVersionApiRequest(
    string ChangeSummary,
    uint ConcurrencyToken);
