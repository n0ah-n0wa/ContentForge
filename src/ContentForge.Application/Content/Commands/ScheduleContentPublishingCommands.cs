namespace ContentForge.Application.Content.Commands;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Abstractions.Scheduling;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Content.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentValidation;

public sealed record ScheduleContentPublishingCommand(
    Guid ContentEntryId,
    DateTimeOffset? PublishAt,
    DateTimeOffset? UnpublishAt,
    ConcurrencyRequest Concurrency);

public sealed class ScheduleContentPublishingCommandValidator : AbstractValidator<ScheduleContentPublishingCommand>
{
    public ScheduleContentPublishingCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class ScheduleContentPublishingCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobScheduler _scheduler;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<ScheduleContentPublishingCommand> _validator;

    public ScheduleContentPublishingCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        IBackgroundJobScheduler scheduler,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IValidator<ScheduleContentPublishingCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduler = scheduler;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(
        ScheduleContentPublishingCommand command,
        CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);

        var timestamp = _clock.UtcNow;
        ApplicationGuard.EnsureConcurrency(entry, command.Concurrency);
        ApplicationGuard.TranslateDomainException(() =>
            entry.SetPublishingSchedule(command.PublishAt, command.UnpublishAt, userId, timestamp));

        if (command.PublishAt is not null)
        {
            await _scheduler.ScheduleContentPublishAsync(
                entry.Id,
                command.PublishAt.Value,
                userId,
                cancellationToken);
        }
        else
        {
            await _scheduler.CancelContentPublishAsync(entry.Id, cancellationToken);
            entry.ClearScheduledPublish(userId, timestamp);
        }

        if (command.UnpublishAt is not null)
        {
            await _scheduler.ScheduleContentUnpublishAsync(
                entry.Id,
                command.UnpublishAt.Value,
                userId,
                cancellationToken);
        }
        else
        {
            await _scheduler.CancelContentUnpublishAsync(entry.Id, cancellationToken);
            entry.ClearScheduledUnpublish(userId, timestamp);
        }

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}

public sealed record ClearContentPublishingScheduleCommand(
    Guid ContentEntryId,
    ConcurrencyRequest Concurrency);

public sealed class ClearContentPublishingScheduleCommandValidator : AbstractValidator<ClearContentPublishingScheduleCommand>
{
    public ClearContentPublishingScheduleCommandValidator()
    {
        RuleFor(command => command.ContentEntryId).NotEmpty();
        RuleFor(command => command.Concurrency.Version).GreaterThan((uint)0);
    }
}

public sealed class ClearContentPublishingScheduleCommandHandler
{
    private readonly IContentEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobScheduler _scheduler;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<ClearContentPublishingScheduleCommand> _validator;

    public ClearContentPublishingScheduleCommandHandler(
        IContentEntryRepository repository,
        IUnitOfWork unitOfWork,
        IBackgroundJobScheduler scheduler,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IValidator<ClearContentPublishingScheduleCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scheduler = scheduler;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
    }

    public async Task<ContentEntryDto> HandleAsync(
        ClearContentPublishingScheduleCommand command,
        CancellationToken cancellationToken)
    {
        await CommandValidator.EnsureValidAsync(_validator, command, cancellationToken);

        var (userId, role) = ApplicationGuard.RequireAuthenticatedUser(_currentUser);
        ApplicationGuard.EnsurePermission(role, Permissions.ContentPublish);

        var entry = await _repository.GetByIdAsync(ContentEntryId.From(command.ContentEntryId), cancellationToken)
            ?? throw new NotFoundApplicationException("ContentEntry", command.ContentEntryId);

        ApplicationGuard.EnsureCanModifyContent(role, userId, entry.CreatedBy);
        ApplicationGuard.EnsureConcurrency(entry, command.Concurrency);

        var timestamp = _clock.UtcNow;
        entry.SetPublishingSchedule(null, null, userId, timestamp);
        await _scheduler.CancelContentPublishAsync(entry.Id, cancellationToken);
        await _scheduler.CancelContentUnpublishAsync(entry.Id, cancellationToken);

        await _repository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ContentEntryMapper.ToDto(entry);
    }
}
