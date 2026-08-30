namespace ContentForge.Api.Controllers;

using ContentForge.Api.Infrastructure;
using ContentForge.Application.Audit.Models;
using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/audit")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AuditController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AuditRead)]
    [ProducesResponseType(typeof(PaginatedResult<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<AuditLogEntryDto>>> List(
        [FromServices] ListAuditLogsQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(
            Request.Query,
            "userId",
            "action",
            "entityType",
            "entityId",
            "from",
            "to");

        var criteria = ListCriteriaFactory.CreateAuditLogListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "timestamp"),
            userId,
            action,
            entityType,
            entityId,
            from,
            to);

        var result = await handler.HandleAsync(new ListAuditLogsQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AuditRead)]
    [ProducesResponseType(typeof(AuditLogEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogEntryDto>> Get(
        Guid id,
        [FromServices] GetAuditLogQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAuditLogQuery(id), cancellationToken);
        return Ok(result);
    }
}
