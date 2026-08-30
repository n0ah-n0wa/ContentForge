namespace ContentForge.Infrastructure.Storage;

using System.Collections.Concurrent;
using ContentForge.Application.Abstractions;
using ContentForge.Domain.Media;

/// <summary>
/// In-memory blob storage for isolated unit tests.
/// </summary>
internal sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public async Task<string> UploadAsync(
        Stream content,
        string contentType,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        _ = contentType;

        var key = FileStorageGuard.RequireKey(storageKey);
        await using var memory = new MemoryStream();
        await FileStorageGuard.CopyLimitedAsync(
            content,
            memory,
            MediaUploadRules.MaxFileSizeBytes,
            cancellationToken).ConfigureAwait(false);

        _files[key.Value] = memory.ToArray();
        return key.Value;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(storageKey);
        _files.TryRemove(key.Value, out _);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(storageKey);
        if (!_files.TryGetValue(key.Value, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(storageKey);
        return Task.FromResult(_files.ContainsKey(key.Value));
    }
}
