namespace ContentForge.Api.Controllers;

using ContentForge.Api.Contracts;
using ContentForge.Api.Infrastructure;
using ContentForge.Application.Authorization;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Query;
using ContentForge.Application.Users;
using ContentForge.Application.Users.Commands;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route($"{ApiConstants.VersionPrefix}/users")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.UserRead)]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<UserDto>>> List(
        [FromServices] ListUsersQueryHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? role = null,
        [FromQuery] string? search = null)
    {
        QueryBinding.EnsureAllowedQueryParameters(Request.Query, "isActive", "role", "search");

        var criteria = ListCriteriaFactory.CreateUserListCriteria(
            QueryBinding.GetPagination(page, pageSize),
            QueryBinding.CreateSort(sortBy, sortDirection, "email"),
            isActive,
            role,
            search);

        var result = await handler.HandleAsync(new ListUsersQuery(criteria), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UserRead)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(
        Guid id,
        [FromServices] GetUserQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UserCreate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserApiRequest request,
        [FromServices] CreateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(UserRequestMapper.ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UserUpdate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserApiRequest request,
        [FromServices] UpdateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(UserRequestMapper.ToCommand(id, request), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = AuthorizationPolicies.UserDisable)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Disable(
        Guid id,
        [FromServices] DisableUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DisableUserCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/enable")]
    [Authorize(Policy = AuthorizationPolicies.UserUpdate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Enable(
        Guid id,
        [FromServices] EnableUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new EnableUserCommand(id), cancellationToken);
        return Ok(result);
    }
}
