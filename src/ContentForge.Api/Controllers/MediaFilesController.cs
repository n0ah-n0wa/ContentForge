namespace ContentForge.Api.Controllers;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

/// <summary>
/// Serves stored media binaries at the public URL prefix configured in <c>Media:PublicBaseUrl</c>.
/// Required for same-origin delivery when blobs are private and nginx proxies <c>/media-files/</c> to the API.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("media-files")]
public sealed class MediaFilesController : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    [HttpGet("{**storageKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        string storageKey,
        [FromServices] IFileStorage fileStorage,
        [FromServices] IMediaRepository mediaRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return NotFound();
        }

        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        var isActive = await mediaRepository
            .ExistsActiveByStorageKeyAsync(normalizedKey, cancellationToken)
            .ConfigureAwait(false);
        if (!isActive)
        {
            return NotFound();
        }

        var stream = await fileStorage.OpenReadAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return NotFound();
        }

        if (!_contentTypeProvider.TryGetContentType(normalizedKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        // SVG can carry executable content; force download instead of inline rendering.
        var forceAttachment = contentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase)
            || normalizedKey.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        if (forceAttachment)
        {
            var fileName = Path.GetFileName(normalizedKey);
            return File(stream, contentType, fileDownloadName: fileName, enableRangeProcessing: true);
        }

        return File(stream, contentType, enableRangeProcessing: true);
    }
}
