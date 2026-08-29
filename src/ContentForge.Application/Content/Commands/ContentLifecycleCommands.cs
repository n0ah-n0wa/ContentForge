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

public sealed record SubmitContentForReviewCommand(Guid ContentEntryId, ConcurrencyRequest Concurrency);

public sealed class SubmitContentForReviewCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public SubmitContentForReviewCommandHandler(
        IContentEntryRepository repository,
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

    public async Task<ContentEntryDto> HandleAsync(SubmitContentForReviewCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentReview);

        var entry = await LoadEntryAsync(command.ContentEntryId, cancellationToken);
        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        ApplicationGuard.TranslateDomainException(() =>
            entry.SubmitForReview(userId, ApplicationGuard.ToDomainToken(command.Concurrency), _clock.UtcNow));

        await PersistAsync(entry, userId, AuditAction.ContentSubmittedForReview, cancellationToken);
        return ContentEntryMapper.ToDto(entry);
    }

    private async Task<ContentEntry> LoadEntryAsync(Guid contentEntryId, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(ContentEntryId.From(contentEntryId), cancellationToken)
        ?? throw new NotFoundApplicationException("ContentEntry", contentEntryId);

    private async Task PersistAsync(ContentEntry entry, UserId userId, AuditAction action, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(action, "ContentEntry", entry.Id.Value.ToString(), userId, cancellationToken: cancellationToken);
    }
}

public sealed record WithdrawContentFromReviewCommand(Guid ContentEntryId, ConcurrencyRequest Concurrency);

public sealed class WithdrawContentFromReviewCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public WithdrawContentFromReviewCommandHandler(
        IContentEntryRepository repository,
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

    public async Task<ContentEntryDto> HandleAsync(WithdrawContentFromReviewCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentReview);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);
        ApplicationGuard.TranslateDomainException(() =>
            entry.WithdrawFromReview(userId, ApplicationGuard.ToDomainToken(command.Concurrency), _clock.UtcNow));

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentWithdrawnFromReview,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

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
    }
}

public sealed class PublishContentCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public PublishContentCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task<ContentEntryDto> HandleAsync(PublishContentCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _contentEntryRepository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", entry.ContentTypeId.Value);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Publish(contentType, userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await _contentEntryRepository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentPublished,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record UnpublishContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class UnpublishContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public UnpublishContentCommandHandler(
        IContentEntryRepository repository,
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

    public async Task<ContentEntryDto> HandleAsync(UnpublishContentCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Unpublish(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentUnpublished,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record ArchiveContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class ArchiveContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public ArchiveContentCommandHandler(
        IContentEntryRepository repository,
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

    public async Task<ContentEntryDto> HandleAsync(ArchiveContentCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentArchive);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.Archive(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentArchived,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record RestoreArchivedContentCommand(
    Guid ContentEntryId,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class RestoreArchivedContentCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public RestoreArchivedContentCommandHandler(
        IContentEntryRepository repository,
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

    public async Task<ContentEntryDto> HandleAsync(RestoreArchivedContentCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRestore);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.TranslateDomainException(() =>
            entry.RestoreFromArchive(userId, ApplicationGuard.ToDomainToken(command.Concurrency), command.ChangeSummary, _clock.UtcNow));

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentRestored,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}
