namespace ContentForge.Api.Controllers;

using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Content.Commands;
using ContentForge.Application.Content.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/content")]
[Authorize]
public sealed class ContentController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ContentCreate)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContentEntryDto>> Create(
        [FromBody] CreateContentApiRequest request,
        [FromServices] CreateContentEntryCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateContentEntryCommand(
                request.ContentTypeId,
                request.Slug,
                request.Data ?? new Dictionary<string, object?>()),
            cancellationToken);

        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = AuthorizationPolicies.ContentPublish)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContentEntryDto>> Publish(
        Guid id,
        [FromBody] PublishContentApiRequest request,
        [FromServices] PublishContentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PublishContentCommand(
                id,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record CreateContentApiRequest(
    Guid ContentTypeId,
    string Slug,
    IReadOnlyDictionary<string, object?>? Data);

public sealed record PublishContentApiRequest(
    string ChangeSummary,
    uint ConcurrencyToken);
