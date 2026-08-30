namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Commands;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

public sealed class AuthCommandTests
{
    [Fact]
    public void LoginCommandValidator_EmptyEmail_FailsValidation()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand { Request = new LoginRequest(string.Empty, "password") };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName.Contains("Email", StringComparison.Ordinal));
    }

    [Fact]
    public void LoginCommandValidator_ValidInput_PassesValidation()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand { Request = new LoginRequest("editor@example.com", "SecurePassword123!") };

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LoginCommandHandler_Success_ReturnsAuthenticatedUser()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var auditService = RepositorySubstituteExtensions.CreateAuditService();
        var handler = new LoginCommandHandler(authenticationService, auditService, new LoginCommandValidator());

        var userId = ApplicationTestData.EditorUserId;
        authenticationService.LoginAsync(Arg.Any<LoginRequest>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new AuthenticationResult(
                userId,
                "editor@example.com",
                "Editor",
                RoleName.Editor,
                "access-token",
                ApplicationTestData.Timestamp.AddHours(1)));

        var result = await handler.HandleAsync(
            new LoginCommand { Request = new LoginRequest("editor@example.com", "SecurePassword123!") },
            CancellationToken.None);

        result.UserId.Should().Be(userId.Value);
        result.Email.Should().Be("editor@example.com");
        result.Role.Should().Be(RoleName.Editor);
        result.AccessToken.Should().Be("access-token");

        await auditService.Received(1).RecordAsync(
            Arg.Any<AuditAction>(),
            "User",
            userId.Value.ToString(),
            userId,
            Arg.Any<string?>(),
            null,
            null,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutCommandHandler_UsesCurrentUserIdentity()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var currentUser = ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.EditorRole);
        var handler = new LogoutCommandHandler(authenticationService, currentUser);

        await handler.HandleAsync(
            new LogoutCommand { RefreshToken = "refresh-token" },
            CancellationToken.None);

        await authenticationService.Received(1).LogoutAsync(
            Arg.Is<LogoutRequest>(request =>
                request.UserId == ApplicationTestData.EditorUserId.Value
                && request.RefreshToken == "refresh-token"),
            Arg.Any<CancellationToken>());
    }
}

public sealed class ValidatorTests
{
    [Fact]
    public void CreateContentEntryCommandValidator_MissingSlug_FailsValidation()
    {
        var validator = new ContentForge.Application.Content.Commands.CreateContentEntryCommandValidator();
        var command = new ContentForge.Application.Content.Commands.CreateContentEntryCommand(
            Guid.NewGuid(),
            string.Empty,
            new Dictionary<string, object?>());

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishContentCommandValidator_MissingChangeSummary_FailsValidation()
    {
        var validator = new ContentForge.Application.Content.Commands.PublishContentCommandValidator();
        var command = new ContentForge.Application.Content.Commands.PublishContentCommand(
            Guid.NewGuid(),
            string.Empty,
            new ContentForge.Application.Common.Concurrency.ConcurrencyRequest(1));

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserCommandValidator_ShortPassword_FailsValidation()
    {
        var validator = new ContentForge.Application.Users.Commands.CreateUserCommandValidator();
        var command = new ContentForge.Application.Users.Commands.CreateUserCommand(
            "user@example.com",
            "User",
            "short",
            RoleName.Viewer);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserCommandValidator_LetterOnlyLongPassword_FailsValidation()
    {
        var validator = new ContentForge.Application.Users.Commands.CreateUserCommandValidator();
        var command = new ContentForge.Application.Users.Commands.CreateUserCommand(
            "user@example.com",
            "User",
            "aaaaaaaaaaaa",
            RoleName.Viewer);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateUserCommandValidator_StrongPassword_PassesValidation()
    {
        var validator = new ContentForge.Application.Users.Commands.CreateUserCommandValidator();
        var command = new ContentForge.Application.Users.Commands.CreateUserCommand(
            "user@example.com",
            "User",
            "SecurePassword123!",
            RoleName.Viewer);

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RestoreContentVersionCommandValidator_InvalidVersion_FailsValidation()
    {
        var validator = new ContentForge.Application.Content.Commands.RestoreContentVersionCommandValidator();
        var command = new ContentForge.Application.Content.Commands.RestoreContentVersionCommand(
            Guid.NewGuid(),
            0,
            "restore",
            new ContentForge.Application.Common.Concurrency.ConcurrencyRequest(1));

        validator.Validate(command).IsValid.Should().BeFalse();
    }
}
