namespace ContentForge.Api.Controllers;

using ContentForge.Api.Contracts;
using ContentForge.Api.Infrastructure;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using ContentForge.Application.Media.Commands;
using ContentForge.Application.Media.Models;
using ContentForge.Application.Media.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/media")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MediaController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MediaRead)]
    [ProducesResponseType(typeof(PaginatedResult<MediaAssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<MediaAssetDto>>> List(
        [FromServices] ListMediaQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? search = null,
        [FromQuery] string? contentType = null,
        [FromQuery] bool? includeDeleted = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(Request.Query, "search", "contentType", "includeDeleted");

        var criteria = ListCriteriaFactory.CreateMediaListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "uploadedAt"),
            search,
            contentType,
            includeDeleted ?? false);

        var result = await handler.HandleAsync(new ListMediaQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MediaRead)]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDto>> Get(
        Guid id,
        [FromServices] GetMediaQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMediaQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MediaUpload)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MediaAssetDto>> Upload(
        IFormFile? file,
        [FromForm] string? altText,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromServices] UploadMediaCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ApplicationValidationException(nameof(file), "File is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(
            new UploadMediaCommand(
                stream,
                file.FileName,
                file.FileName,
                file.ContentType,
                file.Length,
                altText,
                title,
                description),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MediaUpdate)]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetDto>> UpdateMetadata(
        Guid id,
        [FromBody] UpdateMediaMetadataApiRequest request,
        [FromServices] UpdateMediaMetadataCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateMediaMetadataCommand(id, request.AltText, request.Title, request.Description),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MediaDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteMediaCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteMediaCommand(id), cancellationToken);
        return NoContent();
    }
}
