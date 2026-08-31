namespace ContentForge.Api.Controllers;

using ContentForge.Api.Contracts;
using ContentForge.Api.Infrastructure;
using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using ContentForge.Application.Content.Commands;
using ContentForge.Application.ContentPreview.Commands;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Application.ContentPreview.Queries;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Content.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/content")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ContentController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ContentRead)]
    [ProducesResponseType(typeof(PaginatedResult<ContentEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<ContentEntryDto>>> List(
        [FromServices] ListContentEntriesQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] Guid? contentTypeId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? authorId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? includeDeleted = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(
            Request.Query,
            "contentTypeId",
            "status",
            "authorId",
            "search",
            "includeDeleted");

        var criteria = ListCriteriaFactory.CreateContentEntryListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "updatedAt"),
            contentTypeId,
            status,
            authorId,
            search,
            includeDeleted ?? false);

        var result = await handler.HandleAsync(new ListContentEntriesQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthorizationPolicies.ContentRead)]
    [ProducesResponseType(typeof(PaginatedResult<ContentEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<ContentEntryDto>>> Search(
        [FromServices] SearchContentQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? contentTypeId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? authorId = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(
            Request.Query,
            "keyword",
            "contentTypeId",
            "status",
            "authorId",
            "createdFrom",
            "createdTo");

        var criteria = ListCriteriaFactory.CreateContentSearchCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "updatedAt"),
            keyword,
            contentTypeId,
            status,
            authorId,
            createdFrom,
            createdTo);

        var result = await handler.HandleAsync(new SearchContentQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("preview/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ContentPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContentPreviewDto>> GetPreview(
        string token,
        [FromServices] GetContentPreviewQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContentPreviewQuery(token), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentRead)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentEntryDto>> Get(
        Guid id,
        [FromServices] GetContentEntryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContentEntryQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ContentCreate)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentUpdate)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> Update(
        Guid id,
        [FromBody] UpdateContentApiRequest request,
        [FromServices] UpdateContentEntryCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateContentEntryCommand(
                id,
                request.Slug,
                request.Data,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContentDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] DeleteContentApiRequest request,
        [FromServices] DeleteContentEntryCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new DeleteContentEntryCommand(id, new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/submit-for-review")]
    [Authorize(Policy = AuthorizationPolicies.ContentReview)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> SubmitForReview(
        Guid id,
        [FromBody] ContentConcurrencyApiRequest request,
        [FromServices] SubmitContentForReviewCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SubmitContentForReviewCommand(id, new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/withdraw-from-review")]
    [Authorize(Policy = AuthorizationPolicies.ContentReview)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> WithdrawFromReview(
        Guid id,
        [FromBody] ContentConcurrencyApiRequest request,
        [FromServices] WithdrawContentFromReviewCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new WithdrawContentFromReviewCommand(id, new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = AuthorizationPolicies.ContentPublish)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> Publish(
        Guid id,
        [FromBody] ContentLifecycleApiRequest request,
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

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = AuthorizationPolicies.ContentPublish)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> Unpublish(
        Guid id,
        [FromBody] ContentLifecycleApiRequest request,
        [FromServices] UnpublishContentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UnpublishContentCommand(
                id,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AuthorizationPolicies.ContentArchive)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> Archive(
        Guid id,
        [FromBody] ContentLifecycleApiRequest request,
        [FromServices] ArchiveContentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ArchiveContentCommand(
                id,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = AuthorizationPolicies.ContentRestore)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> RestoreArchived(
        Guid id,
        [FromBody] ContentLifecycleApiRequest request,
        [FromServices] RestoreArchivedContentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RestoreArchivedContentCommand(
                id,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/preview-token")]
    [Authorize(Policy = AuthorizationPolicies.ContentRead)]
    [ProducesResponseType(typeof(ContentPreviewTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContentPreviewTokenDto>> CreatePreviewToken(
        Guid id,
        [FromServices] CreateContentPreviewTokenCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateContentPreviewTokenCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/schedule")]
    [Authorize(Policy = AuthorizationPolicies.ContentPublish)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> SchedulePublishing(
        Guid id,
        [FromBody] ScheduleContentPublishingApiRequest request,
        [FromServices] ScheduleContentPublishingCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ScheduleContentPublishingCommand(
                id,
                request.PublishAt,
                request.UnpublishAt,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:guid}/schedule")]
    [Authorize(Policy = AuthorizationPolicies.ContentPublish)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContentEntryDto>> ClearSchedule(
        Guid id,
        [FromBody] ContentConcurrencyApiRequest request,
        [FromServices] ClearContentPublishingScheduleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ClearContentPublishingScheduleCommand(id, new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}/versions")]
    [Authorize(Policy = AuthorizationPolicies.ContentVersionRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ContentVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ContentVersionDto>>> ListVersions(
        Guid id,
        [FromServices] ListContentVersionsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListContentVersionsQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/versions/{versionNumber:int}")]
    [Authorize(Policy = AuthorizationPolicies.ContentVersionRead)]
    [ProducesResponseType(typeof(ContentVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentVersionDto>> GetVersion(
        Guid id,
        int versionNumber,
        [FromServices] GetContentVersionQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContentVersionQuery(id, versionNumber), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/versions/compare")]
    [Authorize(Policy = AuthorizationPolicies.ContentVersionRead)]
    [ProducesResponseType(typeof(ContentVersionComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentVersionComparisonDto>> CompareVersions(
        Guid id,
        [FromQuery] int left,
        [FromQuery] int right,
        [FromServices] CompareContentVersionsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        QueryBinding.EnsureAllowedQueryParameters(Request.Query, "left", "right");

        var result = await handler.HandleAsync(
            new CompareContentVersionsQuery(id, left, right),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/versions/{versionNumber:int}/restore")]
    [Authorize(Policy = AuthorizationPolicies.ContentVersionRestore)]
    [ProducesResponseType(typeof(ContentEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContentEntryDto>> RestoreVersion(
        Guid id,
        int versionNumber,
        [FromBody] RestoreContentVersionApiRequest request,
        [FromServices] RestoreContentVersionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RestoreContentVersionCommand(
                id,
                versionNumber,
                request.ChangeSummary,
                new ConcurrencyRequest(request.ConcurrencyToken)),
            cancellationToken);

        return Ok(result);
    }
}
