namespace ContentForge.IntegrationTests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Auth.Models;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class SecurityAuthRegressionTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public SecurityAuthRegressionTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task CreateUser_WithWeakPassword_Returns422()
    {
        var adminToken = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(new
            {
                email = "weak@contentforge.test",
                displayName = "Weak",
                password = "aaaaaaaaaaaa",
                role = "Viewer",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DisabledUser_AccessTokenIsRejectedImmediately()
    {
        var viewerLogin = await LoginFullAsync(AuthTestConstants.ViewerEmail, AuthTestConstants.ViewerPassword);
        var adminToken = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        using var disableRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/users/{AuthTestConstants.ViewerUserId}/disable");
        disableRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var disableResponse = await _client.SendAsync(disableRequest);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerLogin.AccessToken);
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidatesAccessToken()
    {
        var login = await LoginFullAsync(AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new LogoutApiRequest(login.RefreshToken)),
        };
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var logoutResponse = await _client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshTokenReuse_RevokesTokenFamily()
    {
        var login = await LoginFullAsync(AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);
        var originalRefresh = login.RefreshToken!;

        var rotated = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(originalRefresh));
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedPayload = await rotated.Content.ReadFromJsonAsync<LoginResultDto>();
        rotatedPayload!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var reuse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(originalRefresh));
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var victimRefresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(rotatedPayload.RefreshToken!));
        victimRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Author_CannotEscalateByCreatingAdministrator()
    {
        var authorToken = await LoginAsync(AuthTestConstants.AuthorEmail, AuthTestConstants.AuthorPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(new
            {
                email = "escalated@contentforge.test",
                displayName = "Escalated",
                password = "SecurePassword123!",
                role = "Administrator",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RoleChange_InvalidatesExistingAccessToken()
    {
        var editorLogin = await LoginFullAsync(AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);
        var adminToken = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{AuthTestConstants.EditorUserId}")
        {
            Content = JsonContent.Create(new
            {
                displayName = "Editor",
                role = "Viewer",
            }),
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", editorLogin.AccessToken);
        var meResponse = await _client.SendAsync(meRequest);

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var login = await LoginFullAsync(email, password);
        return login.AccessToken;
    }

    private async Task<LoginResultDto> LoginFullAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LoginResultDto>())!;
    }
}
