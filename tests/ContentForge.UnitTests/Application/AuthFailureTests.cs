namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Auth.Commands;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Audit;
using FluentAssertions;
using NSubstitute;

public sealed class AuthFailureTests
{
    [Fact]
    public async Task LoginCommandHandler_FailedLogin_RecordsAuditAndRethrows()
    {
        var authenticationService = Substitute.For<IAuthenticationService>();
        var auditService = RepositorySubstituteExtensions.CreateAuditService();
        var handler = new LoginCommandHandler(authenticationService, auditService);

        authenticationService.LoginAsync(Arg.Any<LoginRequest>(), null, null, Arg.Any<CancellationToken>())
            .Returns<Task<AuthenticationResult>>(_ => throw new AuthenticationFailedException());

        var action = () => handler.HandleAsync(
            new LoginCommand { Request = new LoginRequest("user@example.com", "WrongPassword123!") },
            CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();

        await auditService.Received(1).RecordAsync(
            AuditAction.LoginFailed,
            "User",
            "user@example.com",
            null,
            null,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
