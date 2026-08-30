namespace ContentForge.Infrastructure.BlobStorage;

using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorage"/>.
/// </summary>
internal sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly IBlobStorageGateway _gateway;
    private readonly string _publicBaseUrl;

    public AzureBlobFileStorage(IBlobStorageGateway gateway, IOptions<MediaStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(options);
        _gateway = gateway;
        _publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(
        Stream content,
        string contentType,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        await _gateway.UploadAsync(storageKey, content, contentType, cancellationToken).ConfigureAwait(false);
        return BuildPublicUrl(storageKey);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        _gateway.DeleteAsync(storageKey, cancellationToken);

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
        _gateway.OpenReadAsync(storageKey, cancellationToken);

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) =>
        _gateway.ExistsAsync(storageKey, cancellationToken);

    private string BuildPublicUrl(string storageKey) => $"{_publicBaseUrl}/{storageKey}";
}
