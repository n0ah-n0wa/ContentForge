namespace ContentForge.Api.Controllers;

using ContentForge.Api.Infrastructure;
using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using ContentForge.Application.ContentTypes.Commands;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.ContentTypes.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/content-types")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ContentTypesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeRead)]
    [ProducesResponseType(typeof(PaginatedResult<ContentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<ContentTypeDto>>> List(
        [FromServices] ListContentTypesQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(Request.Query, "isActive", "search");

        var criteria = ListCriteriaFactory.CreateContentTypeListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "name"),
            isActive,
            search);

        var result = await handler.HandleAsync(new ListContentTypesQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeRead)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentTypeDto>> Get(
        Guid id,
        [FromServices] GetContentTypeQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContentTypeQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeCreate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> Create(
        [FromBody] CreateContentTypeApiRequest request,
        [FromServices] CreateContentTypeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateContentTypeCommand(request.Name, request.DisplayName, request.Slug, request.Description),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> Update(
        Guid id,
        [FromBody] UpdateContentTypeApiRequest request,
        [FromServices] UpdateContentTypeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateContentTypeCommand(id, request.DisplayName, request.Slug, request.Description),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] DeleteContentTypeApiRequest? request,
        [FromServices] DeleteContentTypeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new DeleteContentTypeCommand(id, request?.ConfirmedSafeDeletion ?? false),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/fields")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> AddField(
        Guid id,
        [FromBody] AddContentTypeFieldApiRequest request,
        [FromServices] AddContentTypeFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddContentTypeFieldCommand(
                id,
                request.Name,
                request.FieldType,
                request.DisplayName,
                request.SortOrder,
                request.Configuration),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentTypeDto>> Deactivate(
        Guid id,
        [FromServices] DeactivateContentTypeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeactivateContentTypeCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/fields/{fieldName}")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> UpdateField(
        Guid id,
        string fieldName,
        [FromBody] UpdateContentTypeFieldApiRequest request,
        [FromServices] UpdateContentTypeFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateContentTypeFieldCommand(
                id,
                fieldName,
                request.DisplayName,
                request.SortOrder,
                request.Configuration,
                request.ConfirmedDestructiveChange),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}/fields/{fieldName}")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> RemoveField(
        Guid id,
        string fieldName,
        [FromBody] RemoveContentTypeFieldApiRequest request,
        [FromServices] RemoveContentTypeFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RemoveContentTypeFieldCommand(id, fieldName, request.Confirmed),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}/fields/{fieldName}/name")]
    [Authorize(Policy = AuthorizationPolicies.ContentTypeUpdate)]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentTypeDto>> RenameField(
        Guid id,
        string fieldName,
        [FromBody] RenameContentTypeFieldApiRequest request,
        [FromServices] RenameContentTypeFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RenameContentTypeFieldCommand(id, fieldName, request.NewName, request.Confirmed),
            cancellationToken);

        return Ok(result);
    }
}
