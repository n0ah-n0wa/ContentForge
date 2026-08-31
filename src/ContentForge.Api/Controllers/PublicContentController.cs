namespace ContentForge.Api.Controllers;

using ContentForge.Api.Infrastructure;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Models;
using ContentForge.Application.PublicContent.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentForge.Api.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Anonymous headless API for published content only.
/// </summary>
[ApiController]
[Route($"{ApiConstants.VersionPrefix}/public")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicyNames.PublicApi)]
[Tags("Public Content")]
[Produces("application/json")]
public sealed class PublicContentController : ControllerBase
{
    /// <summary>
    /// Lists published entries for a content type slug.
    /// Draft, in-review, unpublished, archived, and deleted entries are never returned.
    /// </summary>
    [HttpGet("{contentTypeSlug}")]
    [EndpointSummary("List published content")]
    [EndpointDescription(
        "Returns a paginated collection of published content for the given content type slug. " +
        "Unsupported filters are rejected. Administrative fields such as status, author, and audit metadata are not available.")]
    [ProducesResponseType(typeof(PaginatedResult<PublicContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResult<PublicContentDto>>> List(
        string contentTypeSlug,
        [FromServices] ListPublicContentQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? slug = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTimeOffset? publishedFrom = null,
        [FromQuery] DateTimeOffset? publishedTo = null)
    {
        var criteria = ListCriteriaFactory.CreatePublicContentListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "publishedAt", SortDirection.Desc),
            contentTypeSlug,
            slug,
            search,
            publishedFrom,
            publishedTo,
            QueryBinding.CollectUnrecognizedParameters(
                Request.Query,
                "slug",
                "search",
                "publishedFrom",
                "publishedTo"));

        var result = await handler.HandleAsync(new ListPublicContentQuery(criteria), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single published entry by content type slug and entry slug.
    /// Unpublished representations return 404.
    /// </summary>
    [HttpGet("{contentTypeSlug}/{slug}")]
    [EndpointSummary("Get published content by slug")]
    [EndpointDescription(
        "Returns the published snapshot for a content entry. Drafts, in-review, unpublished, and archived entries are indistinguishable from missing content.")]
    [ProducesResponseType(typeof(PublicContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicContentDto>> GetBySlug(
        string contentTypeSlug,
        string slug,
        [FromServices] GetPublicContentBySlugQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetPublicContentBySlugQuery(contentTypeSlug, slug),
            cancellationToken);

        return Ok(result);
    }
}
