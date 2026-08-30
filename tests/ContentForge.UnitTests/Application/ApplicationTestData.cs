namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using NSubstitute;

internal static class ApplicationTestData
{
    internal static UserId EditorUserId { get; } = UserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    internal static UserId AuthorUserId { get; } = UserId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    internal static UserId OtherAuthorUserId { get; } = UserId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    internal static DateTimeOffset Timestamp { get; } = new(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);

    internal static RoleDefinition EditorRole => DefaultRoleDefinitions.Editor;

    internal static RoleDefinition AuthorRole => DefaultRoleDefinitions.Author;

    internal static RoleDefinition AdministratorRole => DefaultRoleDefinitions.Administrator;

    internal static ICurrentUserService CreateCurrentUser(UserId userId, RoleDefinition role)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.Role.Returns(role);
        return currentUser;
    }

    internal static IDateTimeProvider CreateClock() =>
        new FixedDateTimeProvider(Timestamp);

    internal sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

internal static class RepositorySubstituteExtensions
{
    internal static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));
        return unitOfWork;
    }

    internal static IAuditService CreateAuditService() =>
        Substitute.For<IAuditService>();
}
