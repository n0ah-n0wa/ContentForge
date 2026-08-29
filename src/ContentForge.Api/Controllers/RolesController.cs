namespace ContentForge.Api.Controllers;

using ContentForge.Application.Authorization;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.UserRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(
        [FromServices] ListRolesQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var roles = await handler.HandleAsync(new ListRolesQuery(), cancellationToken);
        return Ok(roles);
    }
}
