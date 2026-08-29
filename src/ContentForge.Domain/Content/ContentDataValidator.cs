namespace ContentForge.Domain.Content;

public sealed record ContentValidationError(string Field, string Message);

/// <summary>
/// Validates dynamic content data against a content type schema.
/// </summary>
public static class ContentDataValidator
{
    public static void Validate(ContentTypes.ContentType contentType, ContentData data)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(data);

        var errors = CollectValidationErrors(contentType, data);
        if (errors.Count > 0)
        {
            var message = string.Join(' ', errors.Select(error => $"{error.Field}: {error.Message}"));
            throw new Common.DomainValidationException(message);
        }
    }

    public static IReadOnlyList<ContentValidationError> CollectValidationErrors(
        ContentTypes.ContentType contentType,
        ContentData data)
    {
        var errors = new List<ContentValidationError>();

        foreach (var field in contentType.Fields)
        {
            data.Values.TryGetValue(field.Name.Value, out var value);
            ValidateField(field, value, errors);
        }

        foreach (var unexpectedField in data.Values.Keys.Except(contentType.Fields.Select(field => field.Name.Value), StringComparer.Ordinal))
        {
            errors.Add(new ContentValidationError(unexpectedField, "Field is not defined on the content type."));
        }

        return errors;
    }

    private static void ValidateField(
        ContentTypes.ContentTypeField field,
        object? value,
        List<ContentValidationError> errors)
    {
        if (field.Configuration.IsRequired && IsEmpty(value))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Field is required."));
            return;
        }

        if (IsEmpty(value))
        {
            return;
        }

        switch (field.FieldType)
        {
            case ContentTypes.FieldType.Text or ContentTypes.FieldType.LongText or ContentTypes.FieldType.RichText:
                ValidateString(field, value, errors);
                break;
            case ContentTypes.FieldType.Integer:
                ValidateInteger(field, value, errors);
                break;
            case ContentTypes.FieldType.Decimal:
                ValidateDecimal(field, value, errors);
                break;
            case ContentTypes.FieldType.Boolean:
                ValidateBoolean(field, value, errors);
                break;
            case ContentTypes.FieldType.Date:
                ValidateDate(field, value, errors);
                break;
            case ContentTypes.FieldType.DateTime:
                ValidateDateTime(field, value, errors);
                break;
            case ContentTypes.FieldType.Select:
                ValidateSelect(field, value, errors, allowMultiple: false);
                break;
            case ContentTypes.FieldType.MultiSelect:
                ValidateSelect(field, value, errors, allowMultiple: true);
                break;
            case ContentTypes.FieldType.Media:
                ValidateGuid(field, value, errors);
                break;
            case ContentTypes.FieldType.MediaMultiple:
                ValidateGuidCollection(field, value, errors);
                break;
            case ContentTypes.FieldType.Relation:
                ValidateGuid(field, value, errors);
                break;
            case ContentTypes.FieldType.RelationMultiple:
                ValidateGuidCollection(field, value, errors);
                break;
            case ContentTypes.FieldType.Json:
                ValidateJson(field, value, errors);
                break;
        }
    }

    private static void ValidateString(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not string stringValue)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a string value."));
            return;
        }

        if (field.Configuration.MinLength is not null && stringValue.Length < field.Configuration.MinLength)
        {
            errors.Add(new ContentValidationError(field.Name.Value, $"Minimum length is {field.Configuration.MinLength}."));
        }

        if (field.Configuration.MaxLength is not null && stringValue.Length > field.Configuration.MaxLength)
        {
            errors.Add(new ContentValidationError(field.Name.Value, $"Maximum length is {field.Configuration.MaxLength}."));
        }

        if (field.Configuration.Pattern is not null &&
            !System.Text.RegularExpressions.Regex.IsMatch(stringValue, field.Configuration.Pattern))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Value does not match the required pattern."));
        }
    }

    private static void ValidateInteger(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (!TryConvertToDecimal(value, out var numericValue))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected an integer value."));
            return;
        }

        if (numericValue != decimal.Truncate(numericValue))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a whole number."));
        }

        ValidateNumericRange(field, numericValue, errors);
    }

    private static void ValidateDecimal(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (!TryConvertToDecimal(value, out var numericValue))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a decimal value."));
            return;
        }

        ValidateNumericRange(field, numericValue, errors);
    }

    private static void ValidateNumericRange(
        ContentTypes.ContentTypeField field,
        decimal numericValue,
        List<ContentValidationError> errors)
    {
        if (field.Configuration.MinValue is not null && numericValue < field.Configuration.MinValue)
        {
            errors.Add(new ContentValidationError(field.Name.Value, $"Minimum value is {field.Configuration.MinValue}."));
        }

        if (field.Configuration.MaxValue is not null && numericValue > field.Configuration.MaxValue)
        {
            errors.Add(new ContentValidationError(field.Name.Value, $"Maximum value is {field.Configuration.MaxValue}."));
        }
    }

    private static void ValidateBoolean(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not bool)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a boolean value."));
        }
    }

    private static void ValidateDate(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not DateOnly)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a date value."));
        }
    }

    private static void ValidateDateTime(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not DateTimeOffset and not DateTime)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a date/time value."));
        }
    }

    private static void ValidateSelect(
        ContentTypes.ContentTypeField field,
        object? value,
        List<ContentValidationError> errors,
        bool allowMultiple)
    {
        if (allowMultiple)
        {
            if (value is not IEnumerable<string> values)
            {
                errors.Add(new ContentValidationError(field.Name.Value, "Expected a collection of option values."));
                return;
            }

            foreach (var option in values)
            {
                if (!field.Configuration.Options.Contains(option, StringComparer.Ordinal))
                {
                    errors.Add(new ContentValidationError(field.Name.Value, $"Option '{option}' is not allowed."));
                }
            }

            return;
        }

        if (value is not string optionValue || !field.Configuration.Options.Contains(optionValue, StringComparer.Ordinal))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Value is not an allowed option."));
        }
    }

    private static void ValidateGuid(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not Guid guidValue || guidValue == Guid.Empty)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a non-empty identifier."));
        }
    }

    private static void ValidateGuidCollection(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is not IEnumerable<Guid> values)
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Expected a collection of identifiers."));
            return;
        }

        if (values.Any(id => id == Guid.Empty))
        {
            errors.Add(new ContentValidationError(field.Name.Value, "Collections must not contain empty identifiers."));
        }
    }

    private static void ValidateJson(ContentTypes.ContentTypeField field, object? value, List<ContentValidationError> errors)
    {
        if (value is null || value is string or IReadOnlyDictionary<string, object?> or IEnumerable<object>)
        {
            return;
        }

        errors.Add(new ContentValidationError(field.Name.Value, "Expected a JSON-compatible value."));
    }

    private static bool IsEmpty(object? value) =>
        value is null || (value is string text && string.IsNullOrWhiteSpace(text));

    private static bool TryConvertToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case byte byteValue:
                result = byteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case decimal decimalValue:
                result = decimalValue;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
