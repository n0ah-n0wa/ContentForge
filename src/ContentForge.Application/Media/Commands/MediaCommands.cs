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
    string FileName,
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
        RuleFor(command => command.FileName).NotEmpty();
        RuleFor(command => command.OriginalFileName).NotEmpty();
        RuleFor(command => command.ContentType).NotEmpty();
        RuleFor(command => command.Size).GreaterThan(0);
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

    public UploadMediaCommandHandler(
        IMediaRepository mediaRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService auditService)
    {
        _mediaRepository = mediaRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task<MediaAssetDto> HandleAsync(UploadMediaCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaUpload);

        var mediaId = MediaId.New();
        var extension = Path.GetExtension(command.FileName);
        var storageKey = StorageKey.Create(mediaId.Value, extension);

        var asset = ApplicationGuard.TranslateDomainException(() =>
            MediaAsset.Create(
                command.FileName,
                command.OriginalFileName,
                command.ContentType,
                command.Size,
                storageKey,
                userId,
                uploadedAt: _clock.UtcNow,
                altText: command.AltText,
                title: command.Title,
                description: command.Description));

        await _mediaRepository.AddAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var url = await _fileStorage.UploadAsync(command.Content, command.ContentType, storageKey.Value, cancellationToken);
            asset.UpdateMetadata(command.AltText, command.Title, command.Description, url);
            await _mediaRepository.UpdateAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            asset.MarkDeleted();
            await _mediaRepository.UpdateAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        await _auditService.RecordAsync(
            AuditAction.MediaUploaded,
            "MediaAsset",
            asset.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);

        return MediaMapper.ToDto(asset);
    }
}

public sealed record UpdateMediaMetadataCommand(
    Guid MediaId,
    string? AltText,
    string? Title,
    string? Description);

public sealed class UpdateMediaMetadataCommandHandler
{
    private readonly IMediaRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateMediaMetadataCommandHandler(
        IMediaRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<MediaAssetDto> HandleAsync(UpdateMediaMetadataCommand command, CancellationToken cancellationToken)
    {
        var (_, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaUpdate);

        var asset = await _repository.GetByIdAsync(MediaId.From(command.MediaId), cancellationToken)
            ?? throw new NotFoundApplicationException("MediaAsset", command.MediaId);

        ApplicationGuard.TranslateDomainException(() =>
            asset.UpdateMetadata(command.AltText, command.Title, command.Description));

        await _repository.UpdateAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MediaMapper.ToDto(asset);
    }
}

public sealed record DeleteMediaCommand(Guid MediaId);

public sealed class DeleteMediaCommandHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public DeleteMediaCommandHandler(
        IMediaRepository mediaRepository,
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _mediaRepository = mediaRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task HandleAsync(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.MediaDelete);

        var asset = await _mediaRepository.GetByIdAsync(MediaId.From(command.MediaId), cancellationToken)
            ?? throw new NotFoundApplicationException("MediaAsset", command.MediaId);

        asset.MarkDeleted();
        await _mediaRepository.UpdateAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _fileStorage.DeleteAsync(asset.StorageKey.Value, cancellationToken);

        await _auditService.RecordAsync(
            AuditAction.MediaDeleted,
            "MediaAsset",
            asset.Id.Value.ToString(),
            userId,
            cancellationToken: cancellationToken);
    }
}
