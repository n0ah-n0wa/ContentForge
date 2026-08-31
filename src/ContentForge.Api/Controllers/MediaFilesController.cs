namespace ContentForge.Api.Controllers;

using ContentForge.Application.Abstractions;
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return NotFound();
        }

        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        var stream = await fileStorage.OpenReadAsync(normalizedKey, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return NotFound();
        }

        if (!_contentTypeProvider.TryGetContentType(normalizedKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return File(stream, contentType, enableRangeProcessing: true);
    }
}
