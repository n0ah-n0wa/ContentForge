namespace ContentForge.IntegrationTests.Authorization;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Auth.Models;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class AuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthorizationIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                email = "new@contentforge.test",
                displayName = "New User",
                password = "SecurePassword123!",
                role = "Viewer",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsEditorWithoutUserCreate_Returns403()
    {
        var token = await LoginAsync(AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(new
            {
                email = "blocked@contentforge.test",
                displayName = "Blocked",
                password = "SecurePassword123!",
                role = "Viewer",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsAdministrator_Succeeds()
    {
        var token = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(new
            {
                email = "created-by-admin@contentforge.test",
                displayName = "Created By Admin",
                password = "SecurePassword123!",
                role = "Viewer",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PublishContent_AsAuthor_Returns403()
    {
        var token = await LoginAsync(AuthTestConstants.AuthorEmail, AuthTestConstants.AuthorPassword);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/content/{Guid.NewGuid()}/publish")
        {
            Content = JsonContent.Create(new
            {
                changeSummary = "Attempt publish",
                concurrencyToken = 1u,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublishContent_AsEditor_IsAuthorized()
    {
        var token = await LoginAsync(AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/content/{Guid.NewGuid()}/publish")
        {
            Content = JsonContent.Create(new
            {
                changeSummary = "Publish attempt",
                concurrencyToken = 1u,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Policy allows Editors; missing entry yields Not Found (not Forbidden).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateContent_AsViewer_Returns403()
    {
        var token = await LoginAsync(AuthTestConstants.ViewerEmail, AuthTestConstants.ViewerPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/content")
        {
            Content = JsonContent.Create(new
            {
                contentTypeId = Guid.NewGuid(),
                slug = "viewer-mutation",
                data = new Dictionary<string, object?>(),
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateContent_AsAuthor_IsAuthorized()
    {
        var token = await LoginAsync(AuthTestConstants.AuthorEmail, AuthTestConstants.AuthorPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/content")
        {
            Content = JsonContent.Create(new
            {
                contentTypeId = Guid.NewGuid(),
                slug = "author-draft",
                data = new Dictionary<string, object?>(),
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Policy allows Authors; missing content type yields Not Found (not Forbidden).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListRoles_AsAdministrator_Succeeds()
    {
        var token = await LoginAsync(AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListRoles_AsViewer_Returns403()
    {
        var token = await LoginAsync(AuthTestConstants.ViewerEmail, AuthTestConstants.ViewerPassword);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        return payload!.AccessToken;
    }
}
