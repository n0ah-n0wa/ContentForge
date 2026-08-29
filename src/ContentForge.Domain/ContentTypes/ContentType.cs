namespace ContentForge.Domain.ContentTypes;

using ContentForge.Domain.Common;

/// <summary>
/// Aggregate root describing the schema for a dynamic content type.
/// </summary>
public sealed class ContentType
{
    private readonly List<ContentTypeField> _fields = [];

    private ContentType(
        ContentTypeId id,
        FieldName name,
        string displayName,
        string? description,
        Slug slug,
        bool isActive,
        int version,
        UserId createdBy,
        UserId updatedBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<ContentTypeField> fields)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Description = description;
        Slug = slug;
        IsActive = isActive;
        Version = version;
        CreatedBy = createdBy;
        UpdatedBy = updatedBy;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _fields.AddRange(fields);
        EnsureUniqueFieldNames();
    }

    public ContentTypeId Id { get; }

    public FieldName Name { get; private set; }

    public string DisplayName { get; private set; }

    public string? Description { get; private set; }

    public Slug Slug { get; private set; }

    public bool IsActive { get; private set; }

    public int Version { get; private set; }

    public UserId CreatedBy { get; }

    public UserId UpdatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ContentTypeField> Fields => _fields.AsReadOnly();

    public static ContentType Create(
        FieldName name,
        string displayName,
        Slug slug,
        UserId createdBy,
        string? description = null,
        IEnumerable<ContentTypeField>? fields = null,
        DateTimeOffset? createdAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var timestamp = createdAt ?? DateTimeOffset.UtcNow;
        var fieldList = fields?.ToList() ?? [];

        return new ContentType(
            ContentTypeId.New(),
            name,
            displayName.Trim(),
            description?.Trim(),
            slug,
            isActive: true,
            version: 1,
            createdBy,
            createdBy,
            timestamp,
            timestamp,
            fieldList);
    }

    public static ContentType Restore(
        ContentTypeId id,
        FieldName name,
        string displayName,
        string? description,
        Slug slug,
        bool isActive,
        int version,
        UserId createdBy,
        UserId updatedBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<ContentTypeField> fields) =>
        new(id, name, displayName, description, slug, isActive, version, createdBy, updatedBy, createdAt, updatedAt, fields);

    public void UpdateDetails(
        string displayName,
        string? description,
        Slug slug,
        UserId updatedBy,
        DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        Slug = slug;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        Version++;
    }

    public void Deactivate(UserId updatedBy, DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        Version++;
    }

    public void Activate(UserId updatedBy, DateTimeOffset updatedAt)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        Version++;
    }

    public ContentTypeField AddField(ContentTypeField field, UserId updatedBy, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (_fields.Any(existing => existing.Name == field.Name))
        {
            throw new DomainValidationException(nameof(field), $"Field '{field.Name}' already exists on content type '{Name}'.");
        }

        _fields.Add(field);
        Touch(updatedBy, updatedAt);
        return field;
    }

    public void RemoveField(FieldName fieldName, bool confirmed, UserId updatedBy, DateTimeOffset updatedAt)
    {
        var existing = _fields.SingleOrDefault(field => field.Name == fieldName)
            ?? throw new DomainValidationException(nameof(fieldName), $"Field '{fieldName}' was not found.");

        ContentTypeSchemaEvolution.EnsureRemovalAllowed(existing, confirmed);

        _fields.Remove(existing);
        Touch(updatedBy, updatedAt);
    }

    public void RenameField(FieldName currentName, FieldName newName, bool confirmed, UserId updatedBy, DateTimeOffset updatedAt)
    {
        ContentTypeSchemaEvolution.EnsureRenameAllowed(currentName, newName, confirmed);

        var index = _fields.FindIndex(field => field.Name == currentName);
        if (index < 0)
        {
            throw new DomainValidationException(nameof(currentName), $"Field '{currentName}' was not found.");
        }

        if (_fields.Any(field => field.Name == newName))
        {
            throw new DomainValidationException(nameof(newName), $"Field '{newName}' already exists.");
        }

        var existing = _fields[index];
        _fields[index] = ContentTypeField.Restore(
            existing.Id,
            newName,
            existing.FieldType,
            existing.DisplayName,
            existing.SortOrder,
            existing.Configuration);

        Touch(updatedBy, updatedAt);
    }

    public void ApplyValidationChange(
        FieldName fieldName,
        FieldConfiguration newConfiguration,
        bool confirmed,
        UserId updatedBy,
        DateTimeOffset updatedAt)
    {
        var index = _fields.FindIndex(field => field.Name == fieldName);
        if (index < 0)
        {
            throw new DomainValidationException(nameof(fieldName), $"Field '{fieldName}' was not found.");
        }

        var existing = _fields[index];
        ContentTypeSchemaEvolution.EnsureValidationChangeAllowed(existing, newConfiguration, confirmed);

        _fields[index] = ContentTypeField.Restore(
            existing.Id,
            existing.Name,
            existing.FieldType,
            existing.DisplayName,
            existing.SortOrder,
            newConfiguration);

        Touch(updatedBy, updatedAt);
    }

    public static void EnsureCanDelete(bool hasDependentEntries, bool confirmedSafeDeletion)
    {
        if (hasDependentEntries && !confirmedSafeDeletion)
        {
            throw new InvalidOperationDomainException(
                "Content types with dependent entries require explicit safe deletion confirmation.");
        }
    }

    private void Touch(UserId updatedBy, DateTimeOffset updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
        Version++;
    }

    private void EnsureUniqueFieldNames()
    {
        var duplicates = _fields
            .GroupBy(field => field.Name.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new DomainValidationException(
                nameof(_fields),
                $"Content type '{Name}' contains duplicate field names: {string.Join(", ", duplicates)}.");
        }
    }
}

/// <summary>
/// Domain rules for safe content type schema evolution.
/// </summary>
public static class ContentTypeSchemaEvolution
{
    public static void EnsureRemovalAllowed(ContentTypeField field, bool confirmed)
    {
        if (!confirmed)
        {
            throw new InvalidOperationDomainException(
                $"Removing field '{field.Name}' requires explicit confirmation because existing content may retain legacy values.");
        }
    }

    public static void EnsureRenameAllowed(FieldName currentName, FieldName newName, bool confirmed)
    {
        if (currentName == newName)
        {
            throw new DomainValidationException(nameof(newName), "Renaming a field requires a different target name.");
        }

        if (!confirmed)
        {
            throw new InvalidOperationDomainException(
                $"Renaming field '{currentName}' to '{newName}' requires explicit confirmation.");
        }
    }

    public static void EnsureValidationChangeAllowed(
        ContentTypeField field,
        FieldConfiguration newConfiguration,
        bool confirmed)
    {
        var becameRequired = !field.Configuration.IsRequired && newConfiguration.IsRequired;
        var relationTargetChanged = field.Configuration.RelationTarget != newConfiguration.RelationTarget;

        if ((becameRequired || relationTargetChanged) && !confirmed)
        {
            throw new InvalidOperationDomainException(
                $"Changing validation for field '{field.Name}' requires explicit confirmation because existing content may become invalid.");
        }
    }

    public static bool IsSafeAddition(ContentTypeField field) =>
        !field.Configuration.IsRequired || field.Configuration.DefaultValue is not null;
}
