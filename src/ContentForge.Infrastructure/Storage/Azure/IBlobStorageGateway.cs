namespace ContentForge.Infrastructure.BlobStorage;

/// <summary>
/// Infrastructure port for blob object operations. Enables Azure adapter tests without the SDK surface leaking upward.
/// </summary>
internal interface IBlobStorageGateway
{
    Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);
}
