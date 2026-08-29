using System.Text.RegularExpressions;

namespace ContentForge.Domain.Common;

/// <summary>
/// Represents a normalized, URL-safe slug.
/// </summary>
public sealed partial class Slug : IEquatable<Slug>
{
    private const int _maxLength = 256;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex _slugPattern();

    private Slug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Slug Create(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);

        var normalized = Normalize(rawValue);
        if (normalized.Length == 0)
        {
            throw new DomainValidationException(nameof(rawValue), "Slug cannot be empty after normalization.");
        }

        if (normalized.Length > _maxLength)
        {
            throw new DomainValidationException(nameof(rawValue), $"Slug cannot exceed {_maxLength} characters.");
        }

        if (!_slugPattern().IsMatch(normalized))
        {
            throw new DomainValidationException(nameof(rawValue), "Slug must be URL-safe and contain only lowercase letters, numbers, and hyphens.");
        }

        return new Slug(normalized);
    }

    public static bool TryCreate(string rawValue, out Slug? slug)
    {
        slug = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        try
        {
            slug = Create(rawValue);
            return true;
        }
        catch (DomainValidationException)
        {
            return false;
        }
    }

    public static string Normalize(string rawValue)
    {
        var trimmed = rawValue.Trim().ToLowerInvariant();
        var replaced = Regex.Replace(trimmed, @"[\s_]+", "-");
        replaced = Regex.Replace(replaced, @"[^a-z0-9-]", string.Empty);
        replaced = Regex.Replace(replaced, @"-+", "-");
        return replaced.Trim('-');
    }

    public bool Equals(Slug? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(Slug? left, Slug? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Slug? left, Slug? right) => !(left == right);

    public override bool Equals(object? obj) => obj is Slug other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}
