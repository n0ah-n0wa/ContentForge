namespace ContentForge.Application.Content.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Mapping;
using ContentForge.Application.Media;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentValidation;

public sealed record SubmitContentForReviewCommand(Guid ContentEntryId, ConcurrencyRequest Concurrency);

public sealed class SubmitContentForReviewCommandValidator : AbstractValidator<SubmitContentForReviewCommand>
{
    public SubmitContentForReviewCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class SubmitContentForReviewCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _repository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<SubmitContentForReviewCommand> _validator;

    public SubmitContentForReviewCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository repository,
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<SubmitContentForReviewCommand> validator)
    {
        _contentTypeRepository = contentTypeRepository;
        _repository = repository;
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(SubmitContentForReviewCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentReview);

        var entry = await LoadEntryAsync(command.ContentEntryId, cancellationToken);
        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", entry.ContentTypeId.Value);

        await MediaReferenceValidator.ValidateAsync(contentType, entry.DraftData, _mediaRepository, cancellationToken)
            .ConfigureAwait(false);

        ApplicationGuard.TranslateDomainException(() =>
            entry.SubmitForReview(contentType, userId, ApplicationGuard.ToDomainToken(command.Concurrency), _clock.UtcNow));

        await PersistAsync(entry, userId, AuditAction.ContentSubmittedForReview, cancellationToken);
        return ContentEntryMapper.ToDto(entry);
    }

    private async Task<ContentEntry> LoadEntryAsync(Guid contentEntryId, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(ContentEntryId.From(contentEntryId), cancellationToken)
        ?? throw new NotFoundApplicationException("ContentEntry", contentEntryId);

    private Task PersistAsync(ContentEntry entry, UserId userId, AuditAction action, CancellationToken cancellationToken) =>
        ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            action,
            userId,
            cancellationToken);
}

public sealed record WithdrawContentFromReviewCommand(Guid ContentEntryId, ConcurrencyRequest Concurrency);

public sealed class WithdrawContentFromReviewCommandValidator : AbstractValidator<WithdrawContentFromReviewCommand>
{
    public WithdrawContentFromReviewCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class WithdrawContentFromReviewCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<WithdrawContentFromReviewCommand> _validator;

    public WithdrawContentFromReviewCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<WithdrawContentFromReviewCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(WithdrawContentFromReviewCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentReview);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);
        ApplicationGuard.TranslateDomainException(() =>
            entry.WithdrawFromReview(userId, ApplicationGuard.ToDomainToken(command.Concurrency), _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentWithdrawnFromReview,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record PublishContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class PublishContentCommandValidator : AbstractValidator<PublishContentCommand>
{
    public PublishContentCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class PublishContentCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<PublishContentCommand> _validator;

    public PublishContentCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IMediaRepository mediaRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<PublishContentCommand> validator)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _mediaRepository = mediaRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(PublishContentCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _contentEntryRepository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", entry.ContentTypeId.Value);

        await MediaReferenceValidator.ValidateAsync(contentType, entry.DraftData, _mediaRepository, cancellationToken)
            .ConfigureAwait(false);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Publish(contentType, userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _contentEntryRepository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentPublished,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record UnpublishContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class UnpublishContentCommandValidator : AbstractValidator<UnpublishContentCommand>
{
    public UnpublishContentCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class UnpublishContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<UnpublishContentCommand> _validator;

    public UnpublishContentCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<UnpublishContentCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(UnpublishContentCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Unpublish(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentUnpublished,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record ArchiveContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class ArchiveContentCommandValidator : AbstractValidator<ArchiveContentCommand>
{
    public ArchiveContentCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class ArchiveContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<ArchiveContentCommand> _validator;

    public ArchiveContentCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<ArchiveContentCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(ArchiveContentCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentArchive);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Archive(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentArchived,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record RestoreArchivedContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class RestoreArchivedContentCommandValidator : AbstractValidator<RestoreArchivedContentCommand>
{
    public RestoreArchivedContentCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class RestoreArchivedContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<RestoreArchivedContentCommand> _validator;

    public RestoreArchivedContentCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<RestoreArchivedContentCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(RestoreArchivedContentCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRestore);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.RestoreFromArchive(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentRestored,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}
