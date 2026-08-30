namespace ContentForge.Api.Controllers;

using ContentForge.Application.Dashboard.Models;
using ContentForge.Application.Dashboard.Queries;
using ContentForge.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/dashboard")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class DashboardController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(
        [FromServices] GetDashboardQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDashboardQuery(), cancellationToken);
        return Ok(result);
    }
}
