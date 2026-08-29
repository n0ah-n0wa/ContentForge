namespace ContentForge.Domain.ContentTypes;

using ContentForge.Domain.Common;

/// <summary>
/// Supported dynamic field types.
/// </summary>
public enum FieldType
{
    Text,
    LongText,
    RichText,
    Integer,
    Decimal,
    Boolean,
    Date,
    DateTime,
    Media,
    MediaMultiple,
    Relation,
    RelationMultiple,
    Select,
    MultiSelect,
    Json,
}

/// <summary>
/// Supported relation cardinalities for relation fields.
/// </summary>
public enum RelationCardinality
{
    OneToOne,
    ManyToOne,
    OneToMany,
    ManyToMany,
}

/// <summary>
/// Describes validation and behavior configuration for a field definition.
/// </summary>
public sealed class FieldConfiguration
{
    private FieldConfiguration(
        bool isRequired,
        string? defaultValue,
        int? minLength,
        int? maxLength,
        decimal? minValue,
        decimal? maxValue,
        string? pattern,
        IReadOnlyList<string> options,
        ContentTypeId? relationTarget,
        RelationCardinality? relationCardinality,
        bool allowMultiple)
    {
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        MinLength = minLength;
        MaxLength = maxLength;
        MinValue = minValue;
        MaxValue = maxValue;
        Pattern = pattern;
        Options = options;
        RelationTarget = relationTarget;
        RelationCardinality = relationCardinality;
        AllowMultiple = allowMultiple;
    }

    public bool IsRequired { get; }

    public string? DefaultValue { get; }

    public int? MinLength { get; }

    public int? MaxLength { get; }

    public decimal? MinValue { get; }

    public decimal? MaxValue { get; }

    public string? Pattern { get; }

    public IReadOnlyList<string> Options { get; }

    public ContentTypeId? RelationTarget { get; }

    public RelationCardinality? RelationCardinality { get; }

    public bool AllowMultiple { get; }

    public static FieldConfiguration Create(
        bool isRequired = false,
        string? defaultValue = null,
        int? minLength = null,
        int? maxLength = null,
        decimal? minValue = null,
        decimal? maxValue = null,
        string? pattern = null,
        IEnumerable<string>? options = null,
        ContentTypeId? relationTarget = null,
        RelationCardinality? relationCardinality = null,
        bool allowMultiple = false)
    {
        if (minLength is < 0)
        {
            throw new Common.DomainValidationException(nameof(minLength), "Minimum length cannot be negative.");
        }

        if (maxLength is < 0)
        {
            throw new Common.DomainValidationException(nameof(maxLength), "Maximum length cannot be negative.");
        }

        if (minLength is not null && maxLength is not null && minLength > maxLength)
        {
            throw new Common.DomainValidationException(nameof(maxLength), "Maximum length must be greater than or equal to minimum length.");
        }

        if (minValue is not null && maxValue is not null && minValue > maxValue)
        {
            throw new Common.DomainValidationException(nameof(maxValue), "Maximum value must be greater than or equal to minimum value.");
        }

        var optionList = options?.Where(option => !string.IsNullOrWhiteSpace(option)).Select(option => option.Trim()).Distinct(StringComparer.Ordinal).ToList()
            ?? [];

        return new FieldConfiguration(
            isRequired,
            defaultValue,
            minLength,
            maxLength,
            minValue,
            maxValue,
            pattern,
            optionList,
            relationTarget,
            relationCardinality,
            allowMultiple);
    }
}
