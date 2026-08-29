namespace ContentForge.Domain.Content;

/// <summary>
/// Dynamic field values for a content entry.
/// </summary>
public sealed class ContentData : IEquatable<ContentData>
{
    private readonly Dictionary<string, object?> _values;

    private ContentData(Dictionary<string, object?> values)
    {
        _values = values;
    }

    public IReadOnlyDictionary<string, object?> Values => _values;

    public static ContentData Empty { get; } = new(new Dictionary<string, object?>(StringComparer.Ordinal));

    public static ContentData FromDictionary(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            Common.FieldName.Create(key);
            copy[key] = value;
        }

        return new ContentData(copy);
    }

    public ContentData Clone() => FromDictionary(_values);

    public object? GetValue(string fieldName) =>
        _values.TryGetValue(fieldName, out var value) ? value : null;

    public ContentData WithValue(string fieldName, object? value)
    {
        Common.FieldName.Create(fieldName);
        var copy = new Dictionary<string, object?>(_values, StringComparer.Ordinal)
        {
            [fieldName] = value,
        };

        return new ContentData(copy);
    }

    public bool Equals(ContentData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (_values.Count != other._values.Count)
        {
            return false;
        }

        foreach (var (key, value) in _values)
        {
            if (!other._values.TryGetValue(key, out var otherValue))
            {
                return false;
            }

            if (!Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is ContentData other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var pair in _values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            hash.Add(pair.Key, StringComparer.Ordinal);
            hash.Add(pair.Value);
        }

        return hash.ToHashCode();
    }
}
