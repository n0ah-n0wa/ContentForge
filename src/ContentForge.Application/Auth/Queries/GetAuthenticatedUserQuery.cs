namespace ContentForge.Application.Auth.Queries;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;

public sealed class GetAuthenticatedUserQuery;

public sealed class GetAuthenticatedUserQueryHandler(ICurrentUserService currentUser)
{
    public AuthenticatedUserDto Handle()
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(currentUser);
        var email = currentUser.Email ?? throw new UnauthorizedApplicationException();
        var displayName = currentUser.DisplayName ?? email;

        return new AuthenticatedUserDto(userId.Value, email, displayName, role.Name);
    }
}
