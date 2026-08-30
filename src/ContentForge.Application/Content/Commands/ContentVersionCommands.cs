namespace ContentForge.Application.Content.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentValidation;

public sealed record RestoreContentVersionCommand(
    Guid ContentEntryId,
    int VersionNumber,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class RestoreContentVersionCommandValidator : AbstractValidator<RestoreContentVersionCommand>
{
    public RestoreContentVersionCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.VersionNumber).GreaterThan(0);
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class RestoreContentVersionCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<RestoreContentVersionCommand> _validator;

    public RestoreContentVersionCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<RestoreContentVersionCommand> validator)
    {
        _contentTypeRepository = contentTypeRepository;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(RestoreContentVersionCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentVersionRestore);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", entry.ContentTypeId.Value);

        var sourceVersion = ApplicationGuard.TranslateDomainException(() =>
                entry.GetVersion(VersionNumber.From(command.VersionNumber)))
            ?? throw new NotFoundApplicationException("ContentVersion", command.VersionNumber);

        ApplicationGuard.TranslateDomainException(() =>
            entry.RestoreVersion(
                contentType,
                sourceVersion,
                userId,
                ApplicationGuard.ToDomainToken(command.Concurrency),
                command.ChangeSummary,
                _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentRestored,
            userId,
            cancellationToken,
            metadata: $"restored-from-version={command.VersionNumber}");

        return ContentEntryMapper.ToDto(entry);
    }
}
