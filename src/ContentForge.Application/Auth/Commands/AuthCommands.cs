namespace ContentForge.Application.Auth.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
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
    private readonly IValidator<LoginCommand> _validator;

    public LoginCommandHandler(
        IAuthenticationService authenticationService,
        IAuditService auditService,
        IValidator<LoginCommand> validator)
    {
        _authenticationService = authenticationService;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<LoginResultDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        try
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

            return result.ToLoginResultDto();
        }
        catch (AuthenticationFailedException)
        {
            // Store the attempted account identity for security investigation; never store the password.
            await _auditService.RecordAsync(
                AuditAction.LoginFailed,
                entityType: "User",
                entityId: command.Request.Email.Trim().ToLowerInvariant(),
                userId: null,
                metadata: AuditMetadataSanitizer.Build(("outcome", "failed")),
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent,
                cancellationToken: cancellationToken);

            throw;
        }
    }
}

/// <summary>
/// Invalidates the current authentication session.
/// </summary>
public sealed class LogoutCommand
{
    public string? RefreshToken { get; init; }
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

        await _authenticationService.LogoutAsync(
            new LogoutRequest(userId.Value, command.RefreshToken),
            cancellationToken);
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
    private readonly IValidator<RefreshTokenCommand> _validator;

    public RefreshTokenCommandHandler(
        IAuthenticationService authenticationService,
        IValidator<RefreshTokenCommand> validator)
    {
        _authenticationService = authenticationService;
        _validator = validator;
    }

    public async Task<LoginResultDto> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);
        var result = await _authenticationService.RefreshTokenAsync(command.Request, cancellationToken);
        return result.ToLoginResultDto();
    }
}
