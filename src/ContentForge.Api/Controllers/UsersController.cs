namespace ContentForge.Api.Controllers;

using ContentForge.Application.Authorization;
using ContentForge.Application.Users;
using ContentForge.Application.Users.Commands;
using ContentForge.Application.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UserCreate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserApiRequest request,
        [FromServices] CreateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(UserRequestMapper.ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = AuthorizationPolicies.UserDisable)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> Disable(
        Guid id,
        [FromServices] DisableUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DisableUserCommand(id), cancellationToken);
        return Ok(result);
    }
}
