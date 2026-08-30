namespace ContentForge.Application.ContentTypes.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;
using FluentValidation;

public sealed record CreateContentTypeCommand(
    string Name,
    string DisplayName,
    string Slug,
    string? Description);

public sealed class CreateContentTypeCommandValidator : AbstractValidator<CreateContentTypeCommand>
{
    public CreateContentTypeCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.Slug).NotEmpty();
    }
}

public sealed class CreateContentTypeCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateContentTypeCommand> _validator;

    public CreateContentTypeCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<CreateContentTypeCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(CreateContentTypeCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeCreate);

        var name = ApplicationGuard.CreateFieldName(command.Name);
        var slug = ApplicationGuard.CreateSlug(command.Slug);

        if (await _repository.ExistsByNameAsync(name, cancellationToken))
        {
            throw new ApplicationValidationException(nameof(command.Name), "A content type with this name already exists.");
        }

        if (await _repository.ExistsBySlugAsync(slug, cancellationToken))
        {
            throw new ApplicationValidationException(nameof(command.Slug), "A content type with this slug already exists.");
        }

        var contentType = ContentType.Create(name, command.DisplayName, slug, userId, command.Description, createdAt: _clock.UtcNow);
        await _repository.AddAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeCreated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record UpdateContentTypeCommand(
    Guid ContentTypeId,
    string DisplayName,
    string Slug,
    string? Description);

public sealed class UpdateContentTypeCommandValidator : AbstractValidator<UpdateContentTypeCommand>
{
    public UpdateContentTypeCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.Slug).NotEmpty();
    }
}

public sealed class UpdateContentTypeCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateContentTypeCommand> _validator;

    public UpdateContentTypeCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<UpdateContentTypeCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(UpdateContentTypeCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var slug = ApplicationGuard.CreateSlug(command.Slug);
        if (contentType.Slug != slug)
        {
            var existing = await _repository.GetBySlugAsync(slug, cancellationToken);
            if (existing is not null && existing.Id != contentType.Id)
            {
                throw new ApplicationValidationException(nameof(command.Slug), "A content type with this slug already exists.");
            }
        }

        ApplicationGuard.TranslateDomainException(() =>
            contentType.UpdateDetails(command.DisplayName, command.Description, slug, userId, _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record DeleteContentTypeCommand(Guid ContentTypeId, bool ConfirmedSafeDeletion);

public sealed class DeleteContentTypeCommandValidator : AbstractValidator<DeleteContentTypeCommand>
{
    public DeleteContentTypeCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
    }
}

public sealed class DeleteContentTypeCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<DeleteContentTypeCommand> _validator;

    public DeleteContentTypeCommandHandler(
        IContentTypeRepository repository,
        IContentEntryRepository contentEntryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IValidator<DeleteContentTypeCommand> validator)
    {
        _repository = repository;
        _contentEntryRepository = contentEntryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task HandleAsync(DeleteContentTypeCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeDelete);

        var contentTypeId = ContentTypeId.From(command.ContentTypeId);
        var contentType = await _repository.GetByIdAsync(contentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var hasEntries = await _repository.HasDependentEntriesAsync(contentTypeId, cancellationToken);
        ApplicationGuard.TranslateDomainException(() =>
            ContentType.EnsureCanDelete(hasEntries, command.ConfirmedSafeDeletion));

        if (hasEntries && command.ConfirmedSafeDeletion)
        {
            await _contentEntryRepository.DeleteAllByContentTypeIdAsync(contentTypeId, cancellationToken);
        }

        await _repository.DeleteAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record AddContentTypeFieldCommand(
    Guid ContentTypeId,
    string Name,
    FieldType FieldType,
    string DisplayName,
    int SortOrder,
    Models.FieldConfigurationDto Configuration);

public sealed class AddContentTypeFieldCommandValidator : AbstractValidator<AddContentTypeFieldCommand>
{
    public AddContentTypeFieldCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Configuration).NotNull();
    }
}

public sealed class AddContentTypeFieldCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<AddContentTypeFieldCommand> _validator;

    public AddContentTypeFieldCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<AddContentTypeFieldCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(AddContentTypeFieldCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentTypeId = ContentTypeId.From(command.ContentTypeId);
        var contentType = await _repository.GetByIdAsync(contentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        if (!Enum.IsDefined(command.FieldType))
        {
            throw new ApplicationValidationException(nameof(command.FieldType), "The specified field type is not supported.");
        }

        await ContentTypeRelationValidator.EnsureRelationTargetExistsAsync(
            command.FieldType,
            command.Configuration,
            contentTypeId,
            _repository,
            cancellationToken);

        var configuration = ContentTypeFieldMapper.ToDomainConfiguration(command.Configuration);
        var field = ApplicationGuard.TranslateDomainException(() =>
            ContentTypeField.Create(
                ApplicationGuard.CreateFieldName(command.Name),
                command.FieldType,
                command.DisplayName,
                command.SortOrder,
                configuration));

        ApplicationGuard.TranslateDomainException(() =>
            contentType.AddField(field, userId, _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            metadata: AuditMetadataSanitizer.Build(
                ("change", "fieldAdded"),
                ("fieldName", field.Name.Value)),
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record DeactivateContentTypeCommand(Guid ContentTypeId);

public sealed class DeactivateContentTypeCommandValidator : AbstractValidator<DeactivateContentTypeCommand>
{
    public DeactivateContentTypeCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
    }
}

public sealed class DeactivateContentTypeCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<DeactivateContentTypeCommand> _validator;

    public DeactivateContentTypeCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<DeactivateContentTypeCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(DeactivateContentTypeCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        contentType.Deactivate(userId, _clock.UtcNow);

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record UpdateContentTypeFieldCommand(
    Guid ContentTypeId,
    string FieldName,
    string DisplayName,
    int SortOrder,
    FieldConfigurationDto Configuration,
    bool ConfirmedDestructiveChange);

public sealed class UpdateContentTypeFieldCommandValidator : AbstractValidator<UpdateContentTypeFieldCommand>
{
    public UpdateContentTypeFieldCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.FieldName).NotEmpty();
        RuleFor(command => command.DisplayName).NotEmpty();
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Configuration).NotNull();
    }
}

public sealed class UpdateContentTypeFieldCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<UpdateContentTypeFieldCommand> _validator;

    public UpdateContentTypeFieldCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<UpdateContentTypeFieldCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(UpdateContentTypeFieldCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentTypeId = ContentTypeId.From(command.ContentTypeId);
        var contentType = await _repository.GetByIdAsync(contentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var fieldName = ApplicationGuard.CreateFieldName(command.FieldName);
        var existingField = contentType.Fields.SingleOrDefault(field => field.Name == fieldName)
            ?? throw new NotFoundApplicationException("ContentTypeField", command.FieldName);

        await ContentTypeRelationValidator.EnsureRelationTargetExistsAsync(
            existingField.FieldType,
            command.Configuration,
            contentTypeId,
            _repository,
            cancellationToken);

        var configuration = ContentTypeFieldMapper.ToDomainConfiguration(command.Configuration);
        ApplicationGuard.TranslateDomainException(() =>
            ContentTypeField.EnsureConfigurationValid(existingField.FieldType, configuration));

        ApplicationGuard.TranslateDomainException(() =>
            contentType.UpdateField(
                fieldName,
                command.DisplayName,
                command.SortOrder,
                configuration,
                command.ConfirmedDestructiveChange,
                userId,
                _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            metadata: AuditMetadataSanitizer.Build(
                ("change", "fieldUpdated"),
                ("fieldName", fieldName.Value)),
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record RemoveContentTypeFieldCommand(
    Guid ContentTypeId,
    string FieldName,
    bool Confirmed);

public sealed class RemoveContentTypeFieldCommandValidator : AbstractValidator<RemoveContentTypeFieldCommand>
{
    public RemoveContentTypeFieldCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.FieldName).NotEmpty();
    }
}

public sealed class RemoveContentTypeFieldCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<RemoveContentTypeFieldCommand> _validator;

    public RemoveContentTypeFieldCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<RemoveContentTypeFieldCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(RemoveContentTypeFieldCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var fieldName = ApplicationGuard.CreateFieldName(command.FieldName);
        ApplicationGuard.TranslateDomainException(() =>
            contentType.RemoveField(fieldName, command.Confirmed, userId, _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            metadata: AuditMetadataSanitizer.Build(
                ("change", "fieldRemoved"),
                ("fieldName", fieldName.Value)),
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}

public sealed record RenameContentTypeFieldCommand(
    Guid ContentTypeId,
    string FieldName,
    string NewName,
    bool Confirmed);

public sealed class RenameContentTypeFieldCommandValidator : AbstractValidator<RenameContentTypeFieldCommand>
{
    public RenameContentTypeFieldCommandValidator()
    {
        RuleFor(command => command.ContentTypeId).NotEmpty();
        RuleFor(command => command.FieldName).NotEmpty();
        RuleFor(command => command.NewName).NotEmpty();
    }
}

public sealed class RenameContentTypeFieldCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<RenameContentTypeFieldCommand> _validator;

    public RenameContentTypeFieldCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<RenameContentTypeFieldCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<ContentTypeDto> HandleAsync(RenameContentTypeFieldCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var currentName = ApplicationGuard.CreateFieldName(command.FieldName);
        var newName = ApplicationGuard.CreateFieldName(command.NewName);
        ApplicationGuard.TranslateDomainException(() =>
            contentType.RenameField(currentName, newName, command.Confirmed, userId, _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.ContentTypeUpdated,
            "ContentType",
            contentType.Id.Value.ToString(),
            userId,
            metadata: AuditMetadataSanitizer.Build(
                ("change", "fieldRenamed"),
                ("previousFieldName", currentName.Value),
                ("fieldName", newName.Value)),
            cancellationToken: cancellationToken);

        return ContentTypeMapper.ToDto(contentType);
    }
}
