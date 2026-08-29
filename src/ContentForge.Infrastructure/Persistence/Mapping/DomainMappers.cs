namespace ContentForge.Infrastructure.Persistence.Mapping;

using ContentForge.Application.Users.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Persistence.Converters;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Seed;

internal static class ContentTypeMapper
{
    internal static ContentType ToDomain(ContentTypeEntity entity) =>
        ContentType.Restore(
            ContentTypeId.From(entity.Id),
            FieldName.Create(entity.Name),
            entity.DisplayName,
            entity.Description,
            Slug.Create(entity.Slug),
            entity.IsActive,
            entity.Version,
            UserId.From(entity.CreatedBy),
            UserId.From(entity.UpdatedBy),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Fields
                .OrderBy(field => field.SortOrder)
                .Select(ContentTypeFieldMapper.ToDomain));

    internal static ContentTypeEntity ToEntity(ContentType domain) => new()
    {
        Id = domain.Id.Value,
        Name = domain.Name.Value,
        DisplayName = domain.DisplayName,
        Description = domain.Description,
        Slug = domain.Slug.Value,
        IsActive = domain.IsActive,
        Version = domain.Version,
        CreatedBy = domain.CreatedBy.Value,
        UpdatedBy = domain.UpdatedBy.Value,
        CreatedAt = domain.CreatedAt,
        UpdatedAt = domain.UpdatedAt,
        Fields = domain.Fields
            .Select(field => ContentTypeFieldMapper.ToEntity(field, domain.Id.Value))
            .ToList(),
    };

    internal static void UpdateEntity(ContentTypeEntity entity, ContentType domain)
    {
        entity.Name = domain.Name.Value;
        entity.DisplayName = domain.DisplayName;
        entity.Description = domain.Description;
        entity.Slug = domain.Slug.Value;
        entity.IsActive = domain.IsActive;
        entity.Version = domain.Version;
        entity.UpdatedBy = domain.UpdatedBy.Value;
        entity.UpdatedAt = domain.UpdatedAt;

        var domainFields = domain.Fields.ToDictionary(field => field.Id.Value);
        var existingFields = entity.Fields.ToDictionary(field => field.Id);

        foreach (var existing in entity.Fields.ToList())
        {
            if (!domainFields.ContainsKey(existing.Id))
            {
                entity.Fields.Remove(existing);
            }
        }

        foreach (var domainField in domain.Fields)
        {
            if (existingFields.TryGetValue(domainField.Id.Value, out var existingField))
            {
                if (HasFieldChanged(existingField, domainField, domain.Id.Value))
                {
                    ContentTypeFieldMapper.UpdateEntity(existingField, domainField, domain.Id.Value);
                }
            }
            else
            {
                entity.Fields.Add(ContentTypeFieldMapper.ToEntity(domainField, domain.Id.Value));
            }
        }
    }

    private static bool HasFieldChanged(
        ContentTypeFieldEntity entity,
        ContentTypeField domain,
        Guid contentTypeId)
    {
        var configurationJson = PersistenceJsonConverter.SerializeFieldConfiguration(domain.Configuration);
        return entity.ContentTypeId != contentTypeId
            || entity.Name != domain.Name.Value
            || entity.FieldType != domain.FieldType.ToString()
            || entity.DisplayName != domain.DisplayName
            || entity.SortOrder != domain.SortOrder
            || entity.ConfigurationJson != configurationJson;
    }
}

internal static class ContentTypeFieldMapper
{
    internal static ContentTypeField ToDomain(ContentTypeFieldEntity entity) =>
        ContentTypeField.Restore(
            ContentTypeFieldId.From(entity.Id),
            FieldName.Create(entity.Name),
            Enum.Parse<FieldType>(entity.FieldType),
            entity.DisplayName,
            entity.SortOrder,
            PersistenceJsonConverter.DeserializeFieldConfiguration(entity.ConfigurationJson));

    internal static ContentTypeFieldEntity ToEntity(ContentTypeField domain, Guid contentTypeId) => new()
    {
        Id = domain.Id.Value,
        ContentTypeId = contentTypeId,
        Name = domain.Name.Value,
        FieldType = domain.FieldType.ToString(),
        DisplayName = domain.DisplayName,
        SortOrder = domain.SortOrder,
        ConfigurationJson = PersistenceJsonConverter.SerializeFieldConfiguration(domain.Configuration),
    };

    internal static ContentTypeFieldEntity ToEntity(ContentTypeField domain) =>
        ToEntity(domain, Guid.Empty);

    internal static void UpdateEntity(ContentTypeFieldEntity entity, ContentTypeField domain, Guid contentTypeId)
    {
        entity.ContentTypeId = contentTypeId;
        entity.Name = domain.Name.Value;
        entity.FieldType = domain.FieldType.ToString();
        entity.DisplayName = domain.DisplayName;
        entity.SortOrder = domain.SortOrder;
        entity.ConfigurationJson = PersistenceJsonConverter.SerializeFieldConfiguration(domain.Configuration);
    }
}

internal static class ContentEntryMapper
{
    internal static ContentEntry ToDomain(ContentEntryEntity entity) =>
        ContentEntry.Restore(
            ContentEntryId.From(entity.Id),
            ContentTypeId.From(entity.ContentTypeId),
            Slug.Create(entity.Slug),
            Enum.Parse<ContentStatus>(entity.Status),
            PersistenceJsonConverter.DeserializeContentData(entity.DraftDataJson),
            string.IsNullOrWhiteSpace(entity.PublishedSnapshotJson)
                ? null
                : PersistenceJsonConverter.DeserializeContentSnapshot(entity.PublishedSnapshotJson),
            VersionNumber.From(entity.CurrentVersion),
            new ConcurrencyToken(checked((uint)entity.ConcurrencyToken)),
            UserId.From(entity.CreatedBy),
            UserId.From(entity.UpdatedBy),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.PublishedAt,
            entity.PublishedBy is { } publishedBy ? UserId.From(publishedBy) : null,
            entity.IsDeleted,
            entity.Versions
                .OrderBy(version => version.VersionNumber)
                .Select(ContentVersionMapper.ToDomain));

    internal static ContentEntry ToDomainSummary(ContentEntryEntity entity) =>
        ContentEntry.Restore(
            ContentEntryId.From(entity.Id),
            ContentTypeId.From(entity.ContentTypeId),
            Slug.Create(entity.Slug),
            Enum.Parse<ContentStatus>(entity.Status),
            PersistenceJsonConverter.DeserializeContentData(entity.DraftDataJson),
            string.IsNullOrWhiteSpace(entity.PublishedSnapshotJson)
                ? null
                : PersistenceJsonConverter.DeserializeContentSnapshot(entity.PublishedSnapshotJson),
            VersionNumber.From(entity.CurrentVersion),
            new ConcurrencyToken(checked((uint)entity.ConcurrencyToken)),
            UserId.From(entity.CreatedBy),
            UserId.From(entity.UpdatedBy),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.PublishedAt,
            entity.PublishedBy is { } publishedBy ? UserId.From(publishedBy) : null,
            entity.IsDeleted,
            []);

    internal static ContentEntryEntity ToEntity(ContentEntry domain) => new()
    {
        Id = domain.Id.Value,
        ContentTypeId = domain.ContentTypeId.Value,
        Slug = domain.Slug.Value,
        Status = domain.Status.ToString(),
        DraftDataJson = PersistenceJsonConverter.SerializeContentData(domain.DraftData),
        PublishedSnapshotJson = domain.PublishedSnapshot is null
            ? null
            : PersistenceJsonConverter.SerializeContentSnapshot(domain.PublishedSnapshot),
        CurrentVersion = domain.CurrentVersion.Value,
        ConcurrencyToken = domain.ConcurrencyToken.Value,
        CreatedBy = domain.CreatedBy.Value,
        UpdatedBy = domain.UpdatedBy.Value,
        CreatedAt = domain.CreatedAt,
        UpdatedAt = domain.UpdatedAt,
        PublishedAt = domain.PublishedAt,
        PublishedBy = domain.PublishedBy?.Value,
        IsDeleted = domain.IsDeleted,
        Versions = domain.Versions.Select(ContentVersionMapper.ToEntity).ToList(),
    };

    internal static void UpdateEntity(ContentEntryEntity entity, ContentEntry domain)
    {
        entity.Slug = domain.Slug.Value;
        entity.Status = domain.Status.ToString();
        entity.DraftDataJson = PersistenceJsonConverter.SerializeContentData(domain.DraftData);
        entity.PublishedSnapshotJson = domain.PublishedSnapshot is null
            ? null
            : PersistenceJsonConverter.SerializeContentSnapshot(domain.PublishedSnapshot);
        entity.CurrentVersion = domain.CurrentVersion.Value;
        entity.ConcurrencyToken = domain.ConcurrencyToken.Value;
        entity.UpdatedBy = domain.UpdatedBy.Value;
        entity.UpdatedAt = domain.UpdatedAt;
        entity.PublishedAt = domain.PublishedAt;
        entity.PublishedBy = domain.PublishedBy?.Value;
        entity.IsDeleted = domain.IsDeleted;

        var existingVersionIds = entity.Versions.Select(version => version.Id).ToHashSet();
        foreach (var domainVersion in domain.Versions)
        {
            if (!existingVersionIds.Contains(domainVersion.Id.Value))
            {
                entity.Versions.Add(ContentVersionMapper.ToEntity(domainVersion));
            }
        }
    }
}

internal static class ContentVersionMapper
{
    internal static ContentVersion ToDomain(ContentVersionEntity entity) =>
        ContentVersion.Restore(
            ContentVersionId.From(entity.Id),
            ContentEntryId.From(entity.ContentEntryId),
            VersionNumber.From(entity.VersionNumber),
            PersistenceJsonConverter.DeserializeContentSnapshot(entity.SnapshotJson),
            entity.CreatedAt,
            UserId.From(entity.CreatedBy),
            entity.ChangeSummary);

    internal static ContentVersionEntity ToEntity(ContentVersion domain) => new()
    {
        Id = domain.Id.Value,
        ContentEntryId = domain.ContentEntryId.Value,
        VersionNumber = domain.VersionNumber.Value,
        SnapshotJson = PersistenceJsonConverter.SerializeContentSnapshot(domain.Snapshot),
        CreatedAt = domain.CreatedAt,
        CreatedBy = domain.CreatedBy.Value,
        ChangeSummary = domain.ChangeSummary,
    };
}

internal static class MediaAssetMapper
{
    internal static MediaAsset ToDomain(MediaAssetEntity entity) =>
        MediaAsset.Restore(
            MediaId.From(entity.Id),
            entity.FileName,
            entity.OriginalFileName,
            entity.ContentType,
            entity.Size,
            new StorageKey(entity.StorageKey),
            UserId.From(entity.UploadedBy),
            entity.Url,
            entity.Width,
            entity.Height,
            entity.AltText,
            entity.Title,
            entity.Description,
            entity.UploadedAt,
            entity.IsDeleted);

    internal static MediaAssetEntity ToEntity(MediaAsset domain) => new()
    {
        Id = domain.Id.Value,
        FileName = domain.FileName,
        OriginalFileName = domain.OriginalFileName,
        ContentType = domain.ContentType,
        Size = domain.Size,
        StorageKey = domain.StorageKey.Value,
        Url = domain.Url,
        Width = domain.Width,
        Height = domain.Height,
        AltText = domain.AltText,
        Title = domain.Title,
        Description = domain.Description,
        UploadedBy = domain.UploadedBy.Value,
        UploadedAt = domain.UploadedAt,
        IsDeleted = domain.IsDeleted,
    };

    internal static void UpdateEntity(MediaAssetEntity entity, MediaAsset domain)
    {
        entity.FileName = domain.FileName;
        entity.ContentType = domain.ContentType;
        entity.Url = domain.Url;
        entity.AltText = domain.AltText;
        entity.Title = domain.Title;
        entity.Description = domain.Description;
        entity.IsDeleted = domain.IsDeleted;
    }
}

internal static class AuditLogMapper
{
    internal static AuditLogEntry ToDomain(AuditLogEntity entity) =>
        AuditLogEntry.Restore(
            AuditLogId.From(entity.Id),
            entity.Timestamp,
            entity.UserId is { } userId ? UserId.From(userId) : null,
            Enum.Parse<AuditAction>(entity.Action),
            entity.EntityType,
            entity.EntityId,
            entity.Metadata,
            entity.IpAddress,
            entity.UserAgent);

    internal static AuditLogEntity ToEntity(AuditLogEntry domain) => new()
    {
        Id = domain.Id.Value,
        Timestamp = domain.Timestamp,
        UserId = domain.UserId?.Value,
        Action = domain.Action.ToString(),
        EntityType = domain.EntityType,
        EntityId = domain.EntityId,
        Metadata = domain.Metadata,
        IpAddress = domain.IpAddress,
        UserAgent = domain.UserAgent,
    };
}

internal static class UserAccountMapper
{
    internal static UserAccount ToDomain(ContentForgeUser entity)
    {
        var roleName = entity.UserRoles
            .Select(userRole => userRole.Role.Name)
            .SingleOrDefault()
            ?? RoleName.Viewer.ToString();

        return new UserAccount(
            UserId.From(entity.Id),
            entity.Email ?? string.Empty,
            entity.DisplayName,
            entity.PasswordHash ?? string.Empty,
            entity.IsActive,
            Enum.Parse<RoleName>(roleName),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastLoginAt);
    }

    internal static ContentForgeUser ToEntity(UserAccount domain)
    {
        var normalizedEmail = domain.Email.Trim().ToLowerInvariant();
        return new ContentForgeUser
        {
            Id = domain.Id.Value,
            UserName = normalizedEmail,
            NormalizedUserName = normalizedEmail.ToUpperInvariant(),
            Email = normalizedEmail,
            NormalizedEmail = normalizedEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            DisplayName = domain.DisplayName,
            PasswordHash = domain.PasswordHash,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            LastLoginAt = domain.LastLoginAt,
            LockoutEnabled = true,
            UserRoles =
            [
                new UserRoleEntity
                {
                    UserId = domain.Id.Value,
                    RoleId = AuthorizationSeedIds.RoleId(domain.Role),
                },
            ],
        };
    }

    internal static void UpdateEntity(ContentForgeUser entity, UserAccount domain)
    {
        var normalizedEmail = domain.Email.Trim().ToLowerInvariant();
        entity.UserName = normalizedEmail;
        entity.NormalizedUserName = normalizedEmail.ToUpperInvariant();
        entity.Email = normalizedEmail;
        entity.NormalizedEmail = normalizedEmail.ToUpperInvariant();
        entity.DisplayName = domain.DisplayName;
        entity.PasswordHash = domain.PasswordHash;
        entity.IsActive = domain.IsActive;
        entity.UpdatedAt = domain.UpdatedAt;
        entity.LastLoginAt = domain.LastLoginAt;

        entity.UserRoles.Clear();
        entity.UserRoles.Add(new UserRoleEntity
        {
            UserId = domain.Id.Value,
            RoleId = AuthorizationSeedIds.RoleId(domain.Role),
        });
    }
}

internal static class RoleDefinitionMapper
{
    internal static RoleDefinition ToDomain(RoleEntity entity)
    {
        var roleName = Enum.Parse<RoleName>(entity.Name);
        var permissions = entity.RolePermissions
            .Select(rolePermission => new PermissionName(rolePermission.Permission.Name))
            .ToArray();

        return DefaultRoleDefinitions.All.TryGetValue(roleName, out var definition)
            ? definition
            : RoleDefinition.Create(roleName, permissions);
    }
}
