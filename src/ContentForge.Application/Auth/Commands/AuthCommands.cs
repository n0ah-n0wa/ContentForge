namespace ContentForge.Application.Auth.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common;
using ContentForge.Domain.Audit;
using FluentValidation;

/// <summary>
/// Authenticates a user and returns issued tokens.
/// </summary>
public sealed class LoginCommand
{
    public required LoginRequest Request { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Request.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Request.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuditService _auditService;

    public LoginCommandHandler(IAuthenticationService authenticationService, IAuditService auditService)
    {
        _authenticationService = authenticationService;
        _auditService = auditService;
    }

    public async Task<LoginResultDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            command.Request,
            command.IpAddress,
            command.UserAgent,
            cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.LoginSucceeded,
            entityType: "User",
            entityId: result.UserId.Value.ToString(),
            userId: result.UserId,
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent,
            cancellationToken: cancellationToken);

        return new LoginResultDto(
            result.UserId.Value,
            result.Email,
            result.DisplayName,
            result.Role,
            result.AccessToken,
            result.AccessTokenExpiresAt,
            result.RefreshToken,
            result.RefreshTokenExpiresAt);
    }
}

/// <summary>
/// Invalidates the current authentication session.
/// </summary>
public sealed class LogoutCommand
{
    public required LogoutRequest Request { get; init; }
}

public sealed class LogoutCommandHandler
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUser;

    public LogoutCommandHandler(IAuthenticationService authenticationService, ICurrentUserService currentUser)
    {
        _authenticationService = authenticationService;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var (userId, _) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);

        if (command.Request.UserId != userId)
        {
            throw new Common.Exceptions.ForbiddenApplicationException("Users may only terminate their own session.");
        }

        await _authenticationService.LogoutAsync(command.Request, cancellationToken);
    }
}

/// <summary>
/// Obtains a new access token using a refresh token.
/// </summary>
public sealed class RefreshTokenCommand
{
    public required RefreshTokenRequest Request { get; init; }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.Request.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler
{
    private readonly IAuthenticationService _authenticationService;

    public RefreshTokenCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<AuthenticationResult> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        _authenticationService.RefreshTokenAsync(command.Request, cancellationToken);
}
