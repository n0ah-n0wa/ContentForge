using System.Text.RegularExpressions;

namespace ContentForge.Domain.Common;

/// <summary>
/// Represents a validated content field or type name.
/// </summary>
public sealed partial class FieldName : IEquatable<FieldName>
{
    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex _fieldNamePattern();

    private FieldName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static FieldName Create(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);

        if (!_fieldNamePattern().IsMatch(rawValue))
        {
            throw new DomainValidationException(
                nameof(rawValue),
                "Field names must start with a letter and contain only letters, numbers, and underscores.");
        }

        return new FieldName(rawValue);
    }

    public bool Equals(FieldName? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(FieldName? left, FieldName? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(FieldName? left, FieldName? right) => !(left == right);

    public override bool Equals(object? obj) => obj is FieldName other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    public static implicit operator string(FieldName fieldName) => fieldName.Value;
}
