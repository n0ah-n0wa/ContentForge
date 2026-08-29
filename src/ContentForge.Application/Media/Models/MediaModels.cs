namespace ContentForge.Application.Media.Models;

/// <summary>
/// Media asset metadata returned by administrative queries.
/// </summary>
public sealed record MediaAssetDto(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long Size,
    string StorageKey,
    string? Url,
    int? Width,
    int? Height,
    string? AltText,
    string? Title,
    string? Description,
    Guid UploadedBy,
    DateTimeOffset UploadedAt,
    bool IsDeleted);
