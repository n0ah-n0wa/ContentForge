namespace ContentForge.Application.Mapping;

using ContentForge.Application.Audit.Models;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Application.Content;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.Media.Models;
using ContentForge.Application.Users.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.Domain.Media;

internal static class ContentTypeMapper
{
    internal static ContentTypeDto ToDto(ContentType contentType) =>
        new(
            contentType.Id.Value,
            contentType.Name.Value,
            contentType.DisplayName,
            contentType.Description,
            contentType.Slug.Value,
            contentType.IsActive,
            contentType.Version,
            contentType.CreatedBy.Value,
            contentType.UpdatedBy.Value,
            contentType.CreatedAt,
            contentType.UpdatedAt,
            contentType.Fields.Select(ToFieldDto).ToList(),
            contentType.Fields.Count);

    internal static ContentTypeDto ToListDto(ContentTypeListItem item) =>
        new(
            item.ContentType.Id.Value,
            item.ContentType.Name.Value,
            item.ContentType.DisplayName,
            item.ContentType.Description,
            item.ContentType.Slug.Value,
            item.ContentType.IsActive,
            item.ContentType.Version,
            item.ContentType.CreatedBy.Value,
            item.ContentType.UpdatedBy.Value,
            item.ContentType.CreatedAt,
            item.ContentType.UpdatedAt,
            [],
            item.FieldCount);

    internal static ContentTypeFieldDto ToFieldDto(ContentTypeField field) =>
        new(
            field.Id.Value,
            field.Name.Value,
            field.FieldType,
            field.DisplayName,
            field.SortOrder,
            ToConfigurationDto(field.Configuration));

    internal static FieldConfigurationDto ToConfigurationDto(FieldConfiguration configuration) =>
        new(
            configuration.IsRequired,
            configuration.MinLength,
            configuration.MaxLength,
            configuration.MinValue,
            configuration.MaxValue,
            configuration.Pattern,
            configuration.AllowMultiple,
            configuration.DefaultValue,
            configuration.Options.ToList(),
            configuration.RelationTarget?.Value,
            configuration.RelationCardinality);
}

internal static class ContentEntryMapper
{
    internal static ContentEntryDto ToDto(ContentEntry entry) =>
        new(
            entry.Id.Value,
            entry.ContentTypeId.Value,
            entry.Slug.Value,
            entry.Status,
            entry.DraftData.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            entry.PublishedSnapshot?.Data.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            (uint)entry.CurrentVersion.Value,
            entry.ConcurrencyToken.Value,
            entry.CreatedBy.Value,
            entry.UpdatedBy.Value,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.PublishedAt,
            entry.PublishedBy?.Value,
            entry.ScheduledPublishAt,
            entry.ScheduledUnpublishAt,
            entry.IsDeleted);

    internal static ContentVersionDto ToVersionDto(ContentVersion version) =>
        new(
            version.Id.Value,
            version.ContentEntryId.Value,
            version.VersionNumber.Value,
            version.Snapshot.Slug.Value,
            version.Snapshot.Status,
            version.Snapshot.Data.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            version.CreatedAt,
            version.CreatedBy.Value,
            version.ChangeSummary);

    internal static PublicContentDto ToPublicDto(string contentTypeSlug, ContentEntry entry, ContentType contentType)
    {
        if (entry.PublishedSnapshot is null)
        {
            throw new InvalidOperationException("Published snapshot is required for public content.");
        }

        var publishedData = RichTextSanitizer.SanitizeRichTextFields(contentType, entry.PublishedSnapshot.Data);

        return new PublicContentDto(
            contentTypeSlug,
            entry.PublishedSnapshot.Slug.Value,
            publishedData.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            entry.PublishedAt
                ?? throw new InvalidOperationException("Published content must have a published timestamp."));
    }

    internal static ContentPreviewDto ToPreviewDto(
        string contentTypeSlug,
        ContentEntry entry,
        ContentType contentType,
        DateTimeOffset expiresAt) =>
        new(
            contentTypeSlug,
            entry.Slug.Value,
            entry.Status,
            entry.DraftData.Values.ToDictionary(static pair => pair.Key, static pair => pair.Value),
            contentType.Fields
                .OrderBy(field => field.SortOrder)
                .Select(field => new ContentPreviewFieldDto(
                    field.Name.Value,
                    field.DisplayName,
                    field.FieldType,
                    field.SortOrder))
                .ToList(),
            expiresAt);
}

internal static class MediaMapper
{
    internal static MediaAssetDto ToDto(MediaAsset asset) =>
        new(
            asset.Id.Value,
            asset.FileName,
            asset.OriginalFileName,
            asset.ContentType,
            asset.Size,
            asset.Url,
            asset.Width,
            asset.Height,
            asset.AltText,
            asset.Title,
            asset.Description,
            asset.UploadedBy.Value,
            asset.UploadedAt,
            asset.IsDeleted);
}

internal static class AuditMapper
{
    internal static AuditLogEntryDto ToDto(AuditLogEntry entry) =>
        new(
            entry.Id.Value,
            entry.Timestamp,
            entry.UserId?.Value,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.Metadata,
            entry.IpAddress,
            entry.UserAgent,
            entry.CorrelationId);
}

internal static class UserMapper
{
    internal static UserDto ToDto(UserAccount user) =>
        new(
            user.Id.Value,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt);
}
