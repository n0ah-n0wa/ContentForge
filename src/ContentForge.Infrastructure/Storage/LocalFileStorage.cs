namespace ContentForge.Infrastructure.Storage;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// Local filesystem object storage for development and automated tests.
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IOptions<MediaStorageOptions> options)
        : this(options.Value.LocalRoot, options.Value.PublicBaseUrl)
    {
    }

    internal LocalFileStorage(string root, string publicBaseUrl = "/media-files")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string contentType,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        _ = contentType;

        var key = FileStorageGuard.RequireKey(storageKey);
        var path = ResolvePath(key);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("A storage directory could not be resolved.");
        Directory.CreateDirectory(directory);

        var tempPath = path + ".upload";
        try
        {
            await using (var file = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await FileStorageGuard.CopyLimitedAsync(
                    content,
                    file,
                    MediaUploadRules.MaxFileSizeBytes,
                    cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            throw;
        }

        return BuildUrl(key);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(FileStorageGuard.RequireKey(storageKey));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(FileStorageGuard.RequireKey(storageKey));
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(FileStorageGuard.RequireKey(storageKey));
        return Task.FromResult(File.Exists(path));
    }

    private string ResolvePath(StorageKey key)
    {
        var relative = key.Value.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_root, relative));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, _root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to access a storage path outside the media root.");
        }

        return fullPath;
    }

    private string BuildUrl(StorageKey key) => $"{_publicBaseUrl}/{key.Value}";
}
