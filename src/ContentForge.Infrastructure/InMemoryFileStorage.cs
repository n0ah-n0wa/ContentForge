namespace ContentForge.Infrastructure;

using System.Collections.Concurrent;
using ContentForge.Application.Abstractions;

/// <summary>
/// In-memory blob storage for local and automated test environments.
/// </summary>
internal sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public Task<string> UploadAsync(
        Stream content,
        string contentType,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var memory = new MemoryStream();
        content.CopyTo(memory);
        _files[storageKey] = memory.ToArray();
        return Task.FromResult(storageKey);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(storageKey, out _);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }
}
