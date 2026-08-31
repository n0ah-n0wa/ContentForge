namespace ContentForge.Application.Content.Models;

using ContentForge.Domain.Content;

/// <summary>
/// Administrative content entry details.
/// </summary>
public sealed record ContentEntryDto(
    Guid Id,
    Guid ContentTypeId,
    string Slug,
    ContentStatus Status,
    IReadOnlyDictionary<string, object?> DraftData,
    IReadOnlyDictionary<string, object?>? PublishedData,
    uint CurrentVersion,
    uint ConcurrencyToken,
    Guid CreatedBy,
    Guid UpdatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy,
    DateTimeOffset? ScheduledPublishAt,
    DateTimeOffset? ScheduledUnpublishAt,
    bool IsDeleted);

/// <summary>
/// Immutable content version details.
/// </summary>
public sealed record ContentVersionDto(
    Guid Id,
    Guid ContentEntryId,
    int VersionNumber,
    string Slug,
    ContentStatus Status,
    IReadOnlyDictionary<string, object?> Data,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    string ChangeSummary);

/// <summary>
/// Field-level difference between two content versions.
/// </summary>
public sealed record ContentVersionComparisonDto(
    int LeftVersionNumber,
    int RightVersionNumber,
    IReadOnlyList<ContentFieldChangeDto> Changes);

/// <summary>
/// Single field change between two versions.
/// </summary>
public sealed record ContentFieldChangeDto(string FieldName, object? OldValue, object? NewValue);

/// <summary>
/// Public-facing published content representation.
/// Exposes only the published snapshot — no draft data, audit metadata, user identifiers, or persistence identifiers.
/// </summary>
public sealed record PublicContentDto(
    string ContentTypeSlug,
    string Slug,
    IReadOnlyDictionary<string, object?> Data,
    DateTimeOffset PublishedAt);
