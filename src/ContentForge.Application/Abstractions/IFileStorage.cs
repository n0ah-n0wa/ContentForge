namespace ContentForge.Application.Abstractions;

/// <summary>
/// Stores and retrieves media binaries outside the relational database.
/// </summary>
public interface IFileStorage
{
    Task<string> UploadAsync(
        Stream content,
        string contentType,
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
