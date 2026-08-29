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

    public CreateContentTypeCommandHandler(
        IContentTypeRepository repository,
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

    public async Task<ContentTypeDto> HandleAsync(CreateContentTypeCommand command, CancellationToken cancellationToken)
    {
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

    public UpdateContentTypeCommandHandler(
        IContentTypeRepository repository,
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

    public async Task<ContentTypeDto> HandleAsync(UpdateContentTypeCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        ApplicationGuard.TranslateDomainException(() =>
            contentType.UpdateDetails(command.DisplayName, command.Description, ApplicationGuard.CreateSlug(command.Slug), userId, _clock.UtcNow));

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

public sealed class DeleteContentTypeCommandHandler
{
    private readonly IContentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteContentTypeCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(DeleteContentTypeCommand command, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeDelete);

        var contentTypeId = ContentTypeId.From(command.ContentTypeId);
        var contentType = await _repository.GetByIdAsync(contentTypeId, cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var hasEntries = await _repository.HasDependentEntriesAsync(contentTypeId, cancellationToken);
        ApplicationGuard.TranslateDomainException(() =>
            ContentType.EnsureCanDelete(hasEntries, command.ConfirmedSafeDeletion));

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

    public AddContentTypeFieldCommandHandler(
        IContentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ContentTypeDto> HandleAsync(AddContentTypeFieldCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentTypeUpdate);

        var contentType = await _repository.GetByIdAsync(ContentTypeId.From(command.ContentTypeId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentType", command.ContentTypeId);

        var field = ApplicationGuard.TranslateDomainException(() =>
            ContentTypeField.Create(
                ApplicationGuard.CreateFieldName(command.Name),
                command.FieldType,
                command.DisplayName,
                command.SortOrder,
                ToDomainConfiguration(command.Configuration)));

        ApplicationGuard.TranslateDomainException(() =>
            contentType.AddField(field, userId, _clock.UtcNow));

        await _repository.UpdateAsync(contentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ContentTypeMapper.ToDto(contentType);
    }

    private static FieldConfiguration ToDomainConfiguration(Models.FieldConfigurationDto configuration) =>
        FieldConfiguration.Create(
            isRequired: configuration.IsRequired,
            minLength: configuration.MinLength,
            maxLength: configuration.MaxLength,
            minValue: configuration.MinValue,
            maxValue: configuration.MaxValue,
            pattern: configuration.Pattern,
            allowMultiple: configuration.AllowMultiple,
            defaultValue: configuration.DefaultValue,
            options: configuration.Options,
            relationTarget: configuration.RelationTarget is null ? null : ContentTypeId.From(configuration.RelationTarget.Value),
            relationCardinality: configuration.RelationCardinality);
}
