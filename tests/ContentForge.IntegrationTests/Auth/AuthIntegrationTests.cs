namespace ContentForge.IntegrationTests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Auth.Models;
using ContentForge.Infrastructure.Options;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Collection(PersistenceTests.Name)]
public sealed class AuthIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new ContentForgeWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.Role.Should().Be(Domain.Authorization.RoleName.Administrator);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(AuthTestConstants.AdminEmail, "WrongPassword123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithDisabledAccount_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(AuthTestConstants.DisabledEmail, AuthTestConstants.DisabledPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsAuthenticatedUser()
    {
        var login = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthenticatedUserDto>();
        payload!.Email.Should().Be(AuthTestConstants.AdminEmail);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var jwtOptions = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var expiredToken = AuthTestTokenFactory.CreateExpiredAccessToken(
            jwtOptions,
            AuthTestConstants.AdminUserId,
            AuthTestConstants.AdminEmail,
            Domain.Authorization.RoleName.Administrator.ToString());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AuthTestTokenFactory.CreateInvalidAccessToken());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        var login = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(login.RefreshToken!));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
        payload.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateUserCommandValidator_RejectsWeakPassword()
    {
        var validator = new ContentForge.Application.Users.Commands.CreateUserCommandValidator();
        var command = new ContentForge.Application.Users.Commands.CreateUserCommand(
            "user@example.com",
            "User",
            "short",
            Domain.Authorization.RoleName.Viewer);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    private async Task<LoginResultDto> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LoginResultDto>())!;
    }
}
