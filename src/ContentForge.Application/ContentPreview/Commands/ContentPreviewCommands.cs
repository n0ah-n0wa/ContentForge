namespace ContentForge.Application.ContentPreview.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.Options;

public sealed record CreateContentPreviewTokenCommand(Guid ContentEntryId);

public sealed class CreateContentPreviewTokenCommandValidator : AbstractValidator<CreateContentPreviewTokenCommand>
{
    public CreateContentPreviewTokenCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
    }
}

public sealed class CreateContentPreviewTokenCommandHandler
{
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IContentPreviewTokenRepository _previewTokenRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ContentPreviewOptions _options;
    private readonly IValidator<CreateContentPreviewTokenCommand> _validator;

    public CreateContentPreviewTokenCommandHandler(
        IContentEntryRepository contentEntryRepository,
        IContentPreviewTokenRepository previewTokenRepository,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IOptions<ContentPreviewOptions> options,
        IValidator<CreateContentPreviewTokenCommand> validator)
    {
        _contentEntryRepository = contentEntryRepository;
        _previewTokenRepository = previewTokenRepository;
        _currentUser = currentUser;
        _clock = clock;
        _options = options.Value;
        _validator = validator;
    }

    public async Task<ContentPreviewTokenDto> HandleAsync(
        CreateContentPreviewTokenCommand command,
        CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentRead);

        var entry = ApplicationGuard.RequireVisibleContentEntry(
            await _contentEntryRepository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken),
            command.ContentEntryId);

        ApplicationGuard.EnsureCanReadContent(role, userId, entry.CreatedBy);

        var now = _clock.UtcNow;
        await _previewTokenRepository.RevokeActiveForEntryAsync(entry.Id, now, cancellationToken);

        var expiresAt = now.AddMinutes(_options.TokenLifetimeMinutes);
        var issued = await _previewTokenRepository.IssueAsync(entry.Id, userId, expiresAt, cancellationToken);

        return new ContentPreviewTokenDto(
            issued.Token,
            issued.ExpiresAt,
            $"/api/v1/content/preview/{issued.Token}");
    }
}
