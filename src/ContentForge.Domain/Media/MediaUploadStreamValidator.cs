namespace ContentForge.Domain.Media;

using ContentForge.Domain.Common;

/// <summary>
/// Buffers and validates untrusted upload streams before persistence.
/// </summary>
public static class MediaUploadStreamValidator
{
    public static async Task<MemoryStream> BufferAndValidateAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long declaredSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var isolatedFileName = MediaUploadRules.IsolateFileName(originalFileName);
        var extension = MediaUploadRules.GetExtension(isolatedFileName);
        var normalizedContentType = MediaUploadRules.NormalizeContentType(contentType);
        MediaUploadRules.EnsureAllowed(isolatedFileName, normalizedContentType, declaredSize);

        var buffer = new MemoryStream(capacity: declaredSize > int.MaxValue ? 0 : (int)declaredSize);
        var maxBytes = MediaUploadRules.MaxFileSizeBytes;
        var chunk = new byte[81920];
        long totalRead = 0;

        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            totalRead += read;
            if (totalRead > maxBytes)
            {
                throw new DomainValidationException(
                    nameof(declaredSize),
                    $"Media size must not exceed {maxBytes} bytes.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        if (totalRead == 0)
        {
            throw new DomainValidationException(nameof(declaredSize), "Media size must be greater than zero.");
        }

        if (totalRead != declaredSize)
        {
            throw new DomainValidationException(
                nameof(declaredSize),
                "Declared file size does not match the uploaded stream.");
        }

        MediaContentInspector.EnsureMatchesDeclaredType(buffer.GetBuffer().AsSpan(0, (int)totalRead), extension);
        buffer.Position = 0;
        return buffer;
    }
}
