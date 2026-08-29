namespace ContentForge.Api.Controllers;

using ContentForge.Application.Auth.Commands;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Auth.Queries;
using ContentForge.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(
                new LoginCommand
                {
                    Request = request,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                },
                cancellationToken);

            return Ok(result);
        }
        catch (AuthenticationFailedException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest? request,
        [FromServices] LogoutCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        await handler.HandleAsync(
            new LogoutCommand
            {
                Request = request ?? new LogoutRequest(userId),
            },
            cancellationToken);

        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResult>> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] RefreshTokenCommandHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(
                new RefreshTokenCommand { Request = request },
                cancellationToken);

            return Ok(result);
        }
        catch (AuthenticationFailedException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthenticatedUserDto> Me([FromServices] GetAuthenticatedUserQueryHandler handler)
    {
        try
        {
            return Ok(handler.Handle());
        }
        catch (UnauthorizedApplicationException)
        {
            return Unauthorized();
        }
    }
}
