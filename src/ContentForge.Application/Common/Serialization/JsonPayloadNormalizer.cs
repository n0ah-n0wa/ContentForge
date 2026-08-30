namespace ContentForge.Application.Common.Serialization;

using System.Text.Json;

/// <summary>
/// Normalizes JSON-deserialized dynamic values (for example <see cref="JsonElement"/>) into CLR types.
/// </summary>
public static class JsonPayloadNormalizer
{
    public static IReadOnlyDictionary<string, object?> NormalizeDictionary(
        IReadOnlyDictionary<string, object?> values)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            normalized[key] = NormalizeValue(value);
        }

        return normalized;
    }

    public static object? NormalizeValue(object? value) =>
        value switch
        {
            null => null,
            JsonElement element => NormalizeJsonElement(element),
            _ => value,
        };

    private static object? NormalizeJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => NormalizeJsonElement(item))
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => NormalizeJsonElement(property.Value), StringComparer.Ordinal),
            _ => element.GetRawText(),
        };
}
