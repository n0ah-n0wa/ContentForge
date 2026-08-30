namespace ContentForge.Application.Media.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Mapping;
using ContentForge.Application.Media.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using FluentValidation;

public sealed record UploadMediaCommand(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long Size,
    string? AltText = null,
    string? Title = null,
    string? Description = null);

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(command => command.Content).NotNull();
        RuleFor(command => command.OriginalFileName).NotEmpty();
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.Size).GreaterThan(0).LessThanOrEqualTo(MediaUploadLimits.MaxFileSizeBytes);
    }
}

public sealed class UploadMediaCommandHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;
    private readonly IValidator<UploadMediaCommand> _validator;

    public UploadMediaCommandHandler(
        IMediaRepository mediaRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService,
        IValidator<UploadMediaCommand> validator)
    {
        _mediaRepository = mediaRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task<MediaAssetDto> HandleAsync(UploadMediaCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaUpload);

        MemoryStream validatedStream;
        try
        {
            validatedStream = await MediaUploadStreamValidator.BufferAndValidateAsync(
                command.Content,
                command.OriginalFileName,
                command.ContentType,
                command.Size,
                cancellationToken).ConfigureAwait(false);
        }
        catch (DomainValidationException exception)
        {
            throw new ApplicationValidationException(exception.Field ?? string.Empty, exception.Message);
        }

        await using (validatedStream)
        {
            var originalFileName = ApplicationGuard.TranslateDomainException(() =>
                MediaUploadRules.IsolateFileName(command.OriginalFileName));
            var extension = ApplicationGuard.TranslateDomainException(() =>
                MediaUploadRules.GetExtension(originalFileName));

            var mediaId = MediaId.New();
            var storedFileName = MediaUploadRules.CreateStoredFileName(mediaId.Value, extension);
            var storageKey = ApplicationGuard.TranslateDomainException(() =>
                StorageKey.Create(mediaId.Value, extension, _clock.UtcNow));
            var contentType = MediaUploadRules.NormalizeContentType(command.ContentType);

            string url;
            try
            {
                url = await _fileStorage.UploadAsync(validatedStream, contentType, storageKey.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (DomainValidationException exception)
            {
                throw new ApplicationValidationException(exception.Field ?? string.Empty, exception.Message);
            }

            var asset = ApplicationGuard.TranslateDomainException(() =>
                MediaAsset.Create(
                    mediaId,
                    storedFileName,
                    originalFileName,
                    contentType,
                    command.Size,
                    storageKey,
                    userId,
                    url,
                    altText: command.AltText,
                    title: command.Title,
                    description: command.Description,
                    uploadedAt: _clock.UtcNow));

            try
            {
                await _mediaRepository.AddAsync(asset, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await _fileStorage.DeleteAsync(storageKey.Value, cancellationToken).ConfigureAwait(false);
                throw;
            }

            await _auditService.RecordAsync(
                AuditAction.MediaUploaded,
                "MediaAsset",
                asset.Id.Value.ToString(),
                userId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return MediaMapper.ToDto(asset);
        }
    }
}

public sealed record UpdateMediaMetadataCommand(
    Guid MediaId,
    string? AltText,
    string? Title,
    string? Description);

public sealed class UpdateMediaMetadataCommandValidator : AbstractValidator<UpdateMediaMetadataCommand>
{
    public UpdateMediaMetadataCommandValidator()
    {
        RuleFor(command => command.MediaId).NotEmpty();
        RuleFor(command => command.AltText).MaximumLength(500);
        RuleFor(command => command.Title).MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(2000);
    }
}

public sealed class UpdateMediaMetadataCommandHandler
{
    private readonly IMediaRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<UpdateMediaMetadataCommand> _validator;

    public UpdateMediaMetadataCommandHandler(
        IMediaRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IValidator<UpdateMediaMetadataCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<MediaAssetDto> HandleAsync(UpdateMediaMetadataCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaUpdate);

        var asset = await _repository.GetByIdAsync(MediaId.From(command.MediaId), cancellationToken)
            ?? throw new NotFoundApplicationException("MediaAsset", command.MediaId);

        if (asset.IsDeleted)
        {
            throw new NotFoundApplicationException("MediaAsset", command.MediaId);
        }

        ApplicationGuard.TranslateDomainException(() =>
            asset.UpdateMetadata(command.AltText, command.Title, command.Description));

        await _repository.UpdateAsync(asset, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MediaMapper.ToDto(asset);
    }
}

public sealed record DeleteMediaCommand(Guid MediaId);

public sealed class DeleteMediaCommandValidator : AbstractValidator<DeleteMediaCommand>
{
    public DeleteMediaCommandValidator()
    {
        RuleFor(command => command.MediaId).NotEmpty();
    }
}

public sealed class DeleteMediaCommandHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly IValidator<DeleteMediaCommand> _validator;

    public DeleteMediaCommandHandler(
        IMediaRepository mediaRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService,
        IValidator<DeleteMediaCommand> validator)
    {
        _mediaRepository = mediaRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
        _validator = validator;
    }

    public async Task HandleAsync(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaDelete);

        var asset = await _mediaRepository.GetByIdAsync(MediaId.From(command.MediaId), cancellationToken)
            ?? throw new NotFoundApplicationException("MediaAsset", command.MediaId);

        if (asset.IsDeleted)
        {
            return;
        }

        asset.MarkDeleted();
        await _mediaRepository.UpdateAsync(asset, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _fileStorage.DeleteAsync(asset.StorageKey.Value, cancellationToken).ConfigureAwait(false);

        await _auditService.RecordAsync(
            AuditAction.MediaDeleted,
            "MediaAsset",
            asset.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
