namespace ContentForge.Application.ContentTypes.Models;

using ContentForge.Domain.ContentTypes;

/// <summary>
/// Request body for creating a content type.
/// </summary>
public sealed record CreateContentTypeApiRequest(
    string Name,
    string DisplayName,
    string Slug,
    string? Description);

/// <summary>
/// Request body for updating a content type.
/// </summary>
public sealed record UpdateContentTypeApiRequest(
    string DisplayName,
    string Slug,
    string? Description);

/// <summary>
/// Request body for deleting a content type with dependent-entry confirmation.
/// </summary>
public sealed record DeleteContentTypeApiRequest(bool ConfirmedSafeDeletion);

/// <summary>
/// Request body for adding a field to a content type.
/// </summary>
public sealed record AddContentTypeFieldApiRequest(
    string Name,
    FieldType FieldType,
    string DisplayName,
    int SortOrder,
    FieldConfigurationDto Configuration);

/// <summary>
/// Request body for updating a field on a content type.
/// </summary>
public sealed record UpdateContentTypeFieldApiRequest(
    string DisplayName,
    int SortOrder,
    FieldConfigurationDto Configuration,
    bool ConfirmedDestructiveChange);

/// <summary>
/// Request body for removing a field from a content type.
/// </summary>
public sealed record RemoveContentTypeFieldApiRequest(bool Confirmed);

/// <summary>
/// Request body for renaming a field on a content type.
/// </summary>
public sealed record RenameContentTypeFieldApiRequest(
    string NewName,
    bool Confirmed);
