namespace ContentForge.Application.Content.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Common.Serialization;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentValidation;

public sealed record CreateContentEntryCommand(
    Guid ContentTypeId,
    string Slug,
    IReadOnlyDictionary<string, object?> Data);

public sealed class CreateContentEntryCommandValidator : AbstractValidator<CreateContentEntryCommand>
{
    public CreateContentEntryCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.Slug).NotEmpty();
        RuleFor(command => command.Data).NotNull();
    }
}

public sealed class CreateContentEntryCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateContentEntryCommand> _validator;

    public CreateContentEntryCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<CreateContentEntryCommand> validator)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(CreateContentEntryCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentCreate);

        var contentTypeId = ContentTypeId.From(command.ContentTypeId);
        var contentType = await _contentTypeRepository.GetByIdAsync(contentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var slug = ApplicationGuard.CreateSlug(command.Slug);
        if (await _contentEntryRepository.ExistsBySlugAsync(contentTypeId, slug, cancellationToken))
        {
            throw new ApplicationValidationException(nameof(command.Slug), "An entry with this slug already exists for the content type.");
        }

        var data = ContentData.FromDictionary(JsonPayloadNormalizer.NormalizeDictionary(command.Data));
        ApplicationGuard.TranslateDomainException(() => ContentDataValidator.Validate(contentType, data));

        var entry = ContentEntry.Create(contentTypeId, slug, userId, data, _clock.UtcNow);
        await _contentEntryRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentCreated,
            "ContentEntry",
            entry.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record UpdateContentEntryCommand(
    Guid ContentEntryId,
    string Slug,
    IReadOnlyDictionary<string, object?> Data,
    string ChangeSummary,
    ConcurrencyRequest Concurrency);

public sealed class UpdateContentEntryCommandValidator : AbstractValidator<UpdateContentEntryCommand>
{
    public UpdateContentEntryCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Slug).NotEmpty();
        RuleFor(command => command.ChangeSummary).NotEmpty();
        RuleFor(command => command.Data).NotNull();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class UpdateContentEntryCommandHandler
{
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateContentEntryCommand> _validator;

    public UpdateContentEntryCommandHandler(
        IContentTypeRepository contentTypeRepository,
        IContentEntryRepository contentEntryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<UpdateContentEntryCommand> validator)
    {
        _contentTypeRepository = contentTypeRepository;
        _contentEntryRepository = contentEntryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(UpdateContentEntryCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentUpdate);

        var entry = await _contentEntryRepository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", entry.ContentTypeId.Value);

        var slug = ApplicationGuard.CreateSlug(command.Slug);
        if (!entry.Slug.Equals(slug) &&
            await _contentEntryRepository.ExistsBySlugAsync(entry.ContentTypeId, slug, cancellationToken))
        {
            throw new ApplicationValidationException(nameof(command.Slug), "An entry with this slug already exists for the content type.");
        }

        ApplicationGuard.TranslateDomainException(() =>
            entry.UpdateDraft(
                contentType,
                ContentData.FromDictionary(JsonPayloadNormalizer.NormalizeDictionary(command.Data)),
                slug,
                userId,
                ApplicationGuard.ToDomainToken(command.Concurrency),
                command.ChangeSummary,
                _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _contentEntryRepository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentUpdated,
            userId,
            cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record DeleteContentEntryCommand(Guid ContentEntryId, ConcurrencyRequest Concurrency);

public sealed class DeleteContentEntryCommandValidator : AbstractValidator<DeleteContentEntryCommand>
{
    public DeleteContentEntryCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class DeleteContentEntryCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<DeleteContentEntryCommand> _validator;

    public DeleteContentEntryCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<DeleteContentEntryCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task HandleAsync(DeleteContentEntryCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentDelete);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        ApplicationGuard.TranslateDomainException(() =>
            entry.SoftDelete(userId, ApplicationGuard.ToDomainToken(command.Concurrency), _clock.UtcNow));

        await ContentMutationPersistence.PersistAsync(
            _repository,
            _unitOfWork,
            _auditService,
            entry,
            AuditAction.ContentDeleted,
            userId,
            cancellationToken);
    }
}
