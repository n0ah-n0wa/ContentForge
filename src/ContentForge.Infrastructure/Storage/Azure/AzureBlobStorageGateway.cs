namespace ContentForge.Infrastructure.BlobStorage;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using ContentForge.Infrastructure.Storage;

internal sealed class AzureBlobStorageGateway : IBlobStorageGateway
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageGateway(BlobContainerClient containerClient)
    {
        ArgumentNullException.ThrowIfNull(containerClient);
        _containerClient = containerClient;
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var key = FileStorageGuard.RequireKey(blobName);
        var blobClient = _containerClient.GetBlobClient(key.Value);

        var headers = new BlobHttpHeaders
        {
            ContentType = MediaUploadRules.NormalizeContentType(contentType),
        };

        var options = new BlobUploadOptions
        {
            HttpHeaders = headers,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["storageKey"] = key.Value,
            },
        };

        await using var limited = new LimitedReadStream(content, MediaUploadRules.MaxFileSizeBytes);
        await blobClient.UploadAsync(limited, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(blobName);
        var blobClient = _containerClient.GetBlobClient(key.Value);

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(blobName);
        var blobClient = _containerClient.GetBlobClient(key.Value);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var key = FileStorageGuard.RequireKey(blobName);
        var blobClient = _containerClient.GetBlobClient(key.Value);
        var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    internal async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status is 403 or 409)
        {
            // Container may already exist or be provisioned by infrastructure automation.
        }
    }

    private sealed class LimitedReadStream(Stream inner, long maxBytes) : Stream
    {
        private long _totalRead;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous reads are not supported for upload streams.");

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return 0;
            }

            _totalRead += read;
            if (_totalRead > maxBytes)
            {
                throw new DomainValidationException(
                    "size",
                    $"Media size must not exceed {MediaUploadRules.MaxFileSizeBytes} bytes.");
            }

            return read;
        }

        public override void Flush() => inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
