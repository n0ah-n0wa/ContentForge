namespace ContentForge.Application.Users;

using ContentForge.Application.Users.Commands;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Authorization;

public static class UserRequestMapper
{
    public static CreateUserCommand ToCommand(CreateUserApiRequest request)
    {
        if (!Enum.TryParse<RoleName>(request.Role, ignoreCase: true, out var role))
        {
            throw new ApplicationValidationException(nameof(request.Role), "Role is invalid.");
        }

        return new CreateUserCommand(
            request.Email,
            request.DisplayName,
            request.Password,
            role);
    }
}
