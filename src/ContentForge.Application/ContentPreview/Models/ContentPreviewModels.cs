namespace ContentForge.Application.ContentPreview.Models;

using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Newly issued preview access credentials.
/// </summary>
public sealed record ContentPreviewTokenDto(
    string Token,
    DateTimeOffset ExpiresAt,
    string PreviewPath);

/// <summary>
/// Active preview token loaded from persistence.
/// </summary>
public sealed record ContentPreviewTokenRecord(
    Guid Id,
    Guid ContentEntryId,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Result of issuing a preview token.
/// </summary>
public sealed record ContentPreviewTokenIssueResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Field metadata required to render a preview without administrative API access.
/// </summary>
public sealed record ContentPreviewFieldDto(
    string Name,
    string DisplayName,
    FieldType FieldType,
    int SortOrder);

/// <summary>
/// Scoped draft preview representation without administrative metadata.
/// </summary>
public sealed record ContentPreviewDto(
    string ContentTypeSlug,
    string Slug,
    ContentStatus Status,
    IReadOnlyDictionary<string, object?> Data,
    IReadOnlyList<ContentPreviewFieldDto> Fields,
    DateTimeOffset ExpiresAt);
