namespace ContentForge.Infrastructure.Identity;

using ContentForge.Application.Abstractions;
using ContentForge.Domain.Authorization;
using DomainUserId = ContentForge.Domain.Common.UserId;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

internal sealed class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public DomainUserId? UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var parsedUserId) ? DomainUserId.From(parsedUserId) : null;
        }
    }

    public RoleDefinition? Role
    {
        get
        {
            var roleClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(roleClaim) || !Enum.TryParse<RoleName>(roleClaim, out var roleName))
            {
                return null;
            }

            return DefaultRoleDefinitions.All.TryGetValue(roleName, out var role)
                ? role
                : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public string? DisplayName =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
