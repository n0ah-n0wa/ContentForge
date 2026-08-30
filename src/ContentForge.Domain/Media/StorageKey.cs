namespace ContentForge.Domain.Media;

using System.Text.RegularExpressions;
using ContentForge.Domain.Common;

/// <summary>
/// System-generated object-storage key. Never constructed from a user-supplied path.
/// </summary>
public readonly partial record struct StorageKey
{
    private static readonly Regex _storageKeyPattern = CreateStorageKeyPattern();

    public StorageKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!_storageKeyPattern.IsMatch(value))
        {
            throw new DomainValidationException(
                nameof(value),
                "Storage keys must be system-generated media paths and cannot contain user-supplied locations.");
        }

        Value = value;
    }

    public string Value { get; }

    public static StorageKey Create(Guid identifier, string extension, DateTimeOffset utcNow)
    {
        if (identifier == Guid.Empty)
        {
            throw new DomainValidationException(nameof(identifier), "A storage key identifier is required.");
        }

        var normalizedExtension = MediaUploadRules.NormalizeExtension(extension);
        if (!MediaUploadRules.IsAllowedExtension(normalizedExtension))
        {
            throw new DomainValidationException(nameof(extension), "The file extension is not allowed for media storage.");
        }

        if (normalizedExtension.Contains('/', StringComparison.Ordinal) ||
            normalizedExtension.Contains('\\', StringComparison.Ordinal) ||
            normalizedExtension.Contains("..", StringComparison.Ordinal))
        {
            throw new DomainValidationException(nameof(extension), "Storage key extensions must not contain path separators.");
        }

        return new StorageKey($"media/{utcNow.UtcDateTime:yyyy}/{identifier:N}{normalizedExtension}");
    }

    [GeneratedRegex(@"^media/[0-9]{4}/[0-9a-f]{32}\.[a-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateStorageKeyPattern();
}
