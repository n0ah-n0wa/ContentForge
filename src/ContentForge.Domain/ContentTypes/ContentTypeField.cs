namespace ContentForge.Domain.ContentTypes;

using ContentForge.Domain.Common;

/// <summary>
/// Defines a field within a content type schema.
/// </summary>
public sealed class ContentTypeField
{
    private ContentTypeField(
        ContentTypeFieldId id,
        FieldName name,
        FieldType fieldType,
        string displayName,
        int sortOrder,
        FieldConfiguration configuration)
    {
        Id = id;
        Name = name;
        FieldType = fieldType;
        DisplayName = displayName;
        SortOrder = sortOrder;
        Configuration = configuration;
    }

    public ContentTypeFieldId Id { get; }

    public FieldName Name { get; }

    public FieldType FieldType { get; }

    public string DisplayName { get; }

    public int SortOrder { get; }

    public FieldConfiguration Configuration { get; }

    public static ContentTypeField Create(
        FieldName name,
        FieldType fieldType,
        string displayName,
        int sortOrder,
        FieldConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ValidateFieldTypeConfiguration(fieldType, configuration);

        return new ContentTypeField(
            ContentTypeFieldId.New(),
            name,
            fieldType,
            displayName.Trim(),
            sortOrder,
            configuration);
    }

    internal static ContentTypeField Restore(
        ContentTypeFieldId id,
        FieldName name,
        FieldType fieldType,
        string displayName,
        int sortOrder,
        FieldConfiguration configuration) =>
        new(id, name, fieldType, displayName, sortOrder, configuration);

    public static void EnsureConfigurationValid(FieldType fieldType, FieldConfiguration configuration) =>
        ValidateFieldTypeConfiguration(fieldType, configuration);

    private static void ValidateFieldTypeConfiguration(FieldType fieldType, FieldConfiguration configuration)
    {
        switch (fieldType)
        {
            case FieldType.Select or FieldType.MultiSelect:
                if (configuration.Options.Count == 0)
                {
                    throw new DomainValidationException(nameof(configuration), "Select fields must define at least one option.");
                }

                break;

            case FieldType.Relation or FieldType.RelationMultiple:
                if (configuration.RelationTarget is null)
                {
                    throw new DomainValidationException(nameof(configuration), "Relation fields must define a relation target content type.");
                }

                if (configuration.RelationCardinality is null)
                {
                    throw new DomainValidationException(nameof(configuration), "Relation fields must define a relation cardinality.");
                }

                break;

            case FieldType.MediaMultiple:
                if (!configuration.AllowMultiple)
                {
                    throw new DomainValidationException(nameof(configuration), "MediaMultiple fields must allow multiple values.");
                }

                break;
        }
    }
}
