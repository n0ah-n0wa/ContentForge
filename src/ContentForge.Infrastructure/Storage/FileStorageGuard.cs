namespace ContentForge.Infrastructure.Storage;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;

internal static class FileStorageGuard
{
    internal static StorageKey RequireKey(string storageKey) => new(storageKey);

    internal static async Task CopyLimitedAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new DomainValidationException(
                    "size",
                    $"Media size must not exceed {MediaUploadRules.MaxFileSizeBytes} bytes.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        if (total == 0)
        {
            throw new DomainValidationException("size", "Media size must be greater than zero.");
        }
    }
}
