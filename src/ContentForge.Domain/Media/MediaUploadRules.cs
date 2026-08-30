namespace ContentForge.Domain.Media;

using System.Collections.Frozen;
using ContentForge.Domain.Common;

/// <summary>
/// Upload constraints for media binaries. User-provided names never become storage paths.
/// </summary>
public static class MediaUploadRules
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly FrozenDictionary<string, FrozenSet<string>> _allowedContentTypesByExtension =
        new Dictionary<string, FrozenSet<string>>(StringComparer.Ordinal)
        {
            [".jpg"] = CreateMimeSet("image/jpeg"),
            [".jpeg"] = CreateMimeSet("image/jpeg"),
            [".png"] = CreateMimeSet("image/png"),
            [".gif"] = CreateMimeSet("image/gif"),
            [".webp"] = CreateMimeSet("image/webp"),
            [".svg"] = CreateMimeSet("image/svg+xml"),
            [".pdf"] = CreateMimeSet("application/pdf"),
            [".txt"] = CreateMimeSet("text/plain"),
            [".mp4"] = CreateMimeSet("video/mp4"),
            [".webm"] = CreateMimeSet("video/webm"),
            [".mp3"] = CreateMimeSet("audio/mpeg"),
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    public static bool IsAllowedExtension(string extension) =>
        _allowedContentTypesByExtension.ContainsKey(NormalizeExtension(extension));

    public static void EnsureAllowed(string originalFileName, string contentType, long size)
    {
        if (size <= 0)
        {
            throw new DomainValidationException(nameof(size), "Media size must be greater than zero.");
        }

        if (size > MaxFileSizeBytes)
        {
            throw new DomainValidationException(
                nameof(size),
                $"Media size must not exceed {MaxFileSizeBytes} bytes.");
        }

        var fileName = IsolateFileName(originalFileName);
        var extension = GetExtension(fileName);
        var mimeType = NormalizeContentType(contentType);

        if (!_allowedContentTypesByExtension.TryGetValue(extension, out var allowedMimeTypes)
            || !allowedMimeTypes.Contains(mimeType))
        {
            throw new DomainValidationException(
                nameof(contentType),
                "The file extension and MIME type combination is not allowed.");
        }
    }

    public static string IsolateFileName(string originalFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        var normalized = originalFileName.Replace('\\', '/').Trim();
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new DomainValidationException(
                nameof(originalFileName),
                "Original file names must not contain path traversal sequences.");
        }

        var separator = normalized.LastIndexOf('/');
        var fileName = separator >= 0 ? normalized[(separator + 1)..] : normalized;

        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
        {
            throw new DomainValidationException(nameof(originalFileName), "A file name is required.");
        }

        if (fileName.Contains('\0') || fileName.Any(static character => char.IsControl(character)))
        {
            throw new DomainValidationException(nameof(originalFileName), "The file name contains unsafe control characters.");
        }

        if (fileName.Contains(':', StringComparison.Ordinal))
        {
            throw new DomainValidationException(nameof(originalFileName), "The file name contains unsafe alternate-stream markers.");
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new DomainValidationException(nameof(originalFileName), "The file name contains invalid characters.");
        }

        return fileName;
    }

    public static string GetExtension(string fileName)
    {
        var isolated = IsolateFileName(fileName);
        var separator = isolated.LastIndexOf('.');
        if (separator <= 0 || separator == isolated.Length - 1)
        {
            throw new DomainValidationException(nameof(fileName), "Media files must include a recognized extension.");
        }

        return NormalizeExtension(isolated[separator..]);
    }

    public static string NormalizeContentType(string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var trimmed = contentType.Trim();
        var parameterSeparator = trimmed.IndexOf(';', StringComparison.Ordinal);
        var mimeType = parameterSeparator >= 0 ? trimmed[..parameterSeparator].Trim() : trimmed;
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new DomainValidationException(nameof(contentType), "A MIME type is required.");
        }

        return mimeType.ToLowerInvariant();
    }

    public static string NormalizeExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var normalized = extension.Trim().ToLowerInvariant();
        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        return normalized;
    }

    public static string CreateStoredFileName(Guid mediaId, string extension) =>
        $"{mediaId:N}{NormalizeExtension(extension)}";

    private static FrozenSet<string> CreateMimeSet(params string[] mimeTypes) =>
        mimeTypes.ToFrozenSet(StringComparer.Ordinal);
}
