namespace ContentForge.Application.Content;

using ContentForge.Application.Common.Serialization;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Coerces JSON-deserialized values into schema-aligned CLR types before validation.
/// </summary>
internal static class ContentDataCoercer
{
    internal static ContentData Coerce(ContentType contentType, IReadOnlyDictionary<string, object?> values)
    {
        var normalized = JsonPayloadNormalizer.NormalizeDictionary(values);
        var coerced = new Dictionary<string, object?>(normalized, StringComparer.Ordinal);

        foreach (var field in contentType.Fields)
        {
            if (!coerced.TryGetValue(field.Name.Value, out var value) || value is null)
            {
                continue;
            }

            coerced[field.Name.Value] = field.FieldType switch
            {
                FieldType.Media or FieldType.Relation => CoerceGuid(value),
                FieldType.MediaMultiple or FieldType.RelationMultiple => CoerceGuidCollection(value),
                _ => value,
            };
        }

        return ContentData.FromDictionary(coerced);
    }

    private static object? CoerceGuid(object? value) =>
        value switch
        {
            null => null,
            Guid => value,
            string text when Guid.TryParse(text, out var guid) => guid,
            _ => value,
        };

    private static object CoerceGuidCollection(object value) =>
        value switch
        {
            IEnumerable<Guid> => value,
            IEnumerable<object?> objects => objects.Select(CoerceGuid).ToList()!,
            _ => value,
        };
}
