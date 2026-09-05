namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Users.Commands;
using ContentForge.Domain.Authorization;
using FluentAssertions;
using NSubstitute;

public sealed class UserHandlerTests
{
    [Fact]
    public async Task CreateUserCommandHandler_DuplicateEmail_ThrowsValidationException()
    {
        var repository = Substitute.For<IUserRepository>();
        repository.ExistsByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateUserCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            Substitute.For<IPasswordHasher>(),
            new CreateUserCommandValidator());

        var action = () => handler.HandleAsync(
            new CreateUserCommand("user@example.com", "User", "SecurePassword123!", RoleName.Viewer),
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }

    [Fact]
    public async Task DisableUserCommandHandler_ExistingUser_DisablesAccount()
    {
        var userId = ApplicationTestData.AuthorUserId;
        var user = new ContentForge.Application.Users.Models.UserAccount(
            userId,
            "author@example.com",
            "Author",
            "hashed-password",
            true,
            RoleName.Author,
            ApplicationTestData.Timestamp,
            ApplicationTestData.Timestamp,
            null);

        var repository = Substitute.For<IUserRepository>();
        repository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new DisableUserCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            Substitute.For<ISessionInvalidationService>(),
            new DisableUserCommandValidator());

        var result = await handler.HandleAsync(new DisableUserCommand(userId.Value), CancellationToken.None);

        result.IsActive.Should().BeFalse();
        await repository.Received(1).UpdateAsync(Arg.Is<ContentForge.Application.Users.Models.UserAccount>(account => !account.IsActive), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserCommandHandler_RoleChange_InvalidatesSessions()
    {
        var userId = ApplicationTestData.AuthorUserId;
        var user = new ContentForge.Application.Users.Models.UserAccount(
            userId,
            "author@example.com",
            "Author",
            "hashed-password",
            true,
            RoleName.Author,
            ApplicationTestData.Timestamp,
            ApplicationTestData.Timestamp,
            null);

        var repository = Substitute.For<IUserRepository>();
        repository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var sessionInvalidation = Substitute.For<ISessionInvalidationService>();

        var handler = new UpdateUserCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            sessionInvalidation);

        await handler.HandleAsync(
            new UpdateUserCommand(userId.Value, "Author", RoleName.Viewer),
            CancellationToken.None);

        await sessionInvalidation.Received(1).InvalidateUserSessionsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableUserCommandHandler_DisabledUser_EnablesAccount()
    {
        var userId = ApplicationTestData.AuthorUserId;
        var user = new ContentForge.Application.Users.Models.UserAccount(
            userId,
            "author@example.com",
            "Author",
            "hashed-password",
            false,
            RoleName.Author,
            ApplicationTestData.Timestamp,
            ApplicationTestData.Timestamp,
            null);

        var repository = Substitute.For<IUserRepository>();
        repository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new EnableUserCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new EnableUserCommandValidator());

        var result = await handler.HandleAsync(new EnableUserCommand(userId.Value), CancellationToken.None);

        result.IsActive.Should().BeTrue();
        await repository.Received(1).UpdateAsync(Arg.Is<ContentForge.Application.Users.Models.UserAccount>(account => account.IsActive), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableUserCommandHandler_EmptyUserId_ThrowsValidationException()
    {
        var handler = new DisableUserCommandHandler(
            Substitute.For<IUserRepository>(),
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            Substitute.For<ISessionInvalidationService>(),
            new DisableUserCommandValidator());

        var action = () => handler.HandleAsync(new DisableUserCommand(Guid.Empty), CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }
}
