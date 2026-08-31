namespace ContentForge.Application.ContentTypes.Models;

using ContentForge.Domain.ContentTypes;

/// <summary>
/// Field configuration details exposed through the application layer.
/// </summary>
public sealed record FieldConfigurationDto(
    bool IsRequired,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? Pattern,
    bool AllowMultiple,
    string? DefaultValue,
    IReadOnlyList<string> Options,
    Guid? RelationTarget,
    RelationCardinality? RelationCardinality);

/// <summary>
/// Content type field details exposed through the application layer.
/// </summary>
public sealed record ContentTypeFieldDto(
    Guid Id,
    string Name,
    FieldType FieldType,
    string DisplayName,
    int SortOrder,
    FieldConfigurationDto Configuration);

/// <summary>
/// Content type details returned by administrative queries.
/// </summary>
public sealed record ContentTypeDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    string Slug,
    bool IsActive,
    int Version,
    Guid CreatedBy,
    Guid UpdatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ContentTypeFieldDto> Fields,
    int FieldCount);
