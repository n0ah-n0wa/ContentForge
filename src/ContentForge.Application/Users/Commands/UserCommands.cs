namespace ContentForge.Application.Users.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Validation;
using ContentForge.Application.Mapping;
using ContentForge.Application.Users.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using FluentValidation;

public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string Password,
    RoleName Role);

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.Password).ApplyPasswordPolicy();
        RuleFor(command => command.Role).IsInEnum();
    }
}

public sealed class CreateUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateUserCommand> _validator;

    public CreateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IPasswordHasher passwordHasher,
        IValidator<CreateUserCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
        _validator = validator;
    }

    public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (actorId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserCreate);

        if (await _repository.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            throw new ApplicationValidationException(nameof(command.Email), "A user with this email already exists.");
        }

        var timestamp = _clock.UtcNow;
        var user = new UserAccount(
            UserId.From(Guid.NewGuid()),
            command.Email.Trim(),
            command.DisplayName.Trim(),
            _passwordHasher.HashPassword(command.Password),
            IsActive: true,
            command.Role,
            timestamp,
            timestamp,
            LastLoginAt: null);

        await _repository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.UserCreated,
            "User",
            user.Id.Value.ToString(),
            actorId,
            cancellationToken: cancellationToken);

        return UserMapper.ToDto(user);
    }
}

public sealed record UpdateUserCommand(Guid UserId, string DisplayName, RoleName Role);

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.Role).IsInEnum();
    }
}

public sealed class UpdateUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public UpdateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task<UserDto> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var (actorId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserUpdate);

        var user = await _repository.GetByIdAsync(UserId.From(command.UserId), cancellationToken)
            ?? throw new NotFoundApplicationException("User", command.UserId);

        var previousRole = user.Role;
        user = user.WithDisplayName(command.DisplayName, _clock.UtcNow).WithRole(command.Role, _clock.UtcNow);

        await _repository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousRole != user.Role)
        {
            await _auditService.RecordAsync(
                AuditAction.UserRoleChanged,
                "User",
                user.Id.Value.ToString(),
                actorId,
                metadata: AuditMetadataSanitizer.Build(
                    ("previousRole", previousRole.ToString()),
                    ("newRole", user.Role.ToString())),
                cancellationToken: cancellationToken);
        }

        return UserMapper.ToDto(user);
    }
}

public sealed record DisableUserCommand(Guid UserId);

public sealed class DisableUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly ISessionInvalidationService _sessionInvalidation;

    public DisableUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        ISessionInvalidationService sessionInvalidation)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _sessionInvalidation = sessionInvalidation;
    }

    public async Task<UserDto> HandleAsync(DisableUserCommand command, CancellationToken cancellationToken)
    {
        var (actorId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.UserDisable);

        var user = await _repository.GetByIdAsync(UserId.From(command.UserId), cancellationToken)
            ?? throw new NotFoundApplicationException("User", command.UserId);

        if (user.Id == actorId)
        {
            throw new ApplicationValidationException(nameof(command.UserId), "Users cannot disable their own account.");
        }

        user = user.Disable(_clock.UtcNow);
        await _repository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _sessionInvalidation.InvalidateUserSessionsAsync(user.Id, cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.UserDisabled,
            "User",
            user.Id.Value.ToString(),
            actorId,
            cancellationToken: cancellationToken);

        return UserMapper.ToDto(user);
    }
}
