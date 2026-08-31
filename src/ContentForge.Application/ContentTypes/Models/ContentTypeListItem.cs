namespace ContentForge.Application.ContentTypes.Models;

using ContentForge.Domain.ContentTypes;

/// <summary>
/// Lightweight content type row for paginated list queries.
/// </summary>
public sealed record ContentTypeListItem(ContentType ContentType, int FieldCount);
