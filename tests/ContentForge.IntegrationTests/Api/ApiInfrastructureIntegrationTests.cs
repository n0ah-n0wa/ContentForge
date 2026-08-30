namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ApiInfrastructureIntegrationTests : IAsyncLifetime
{
    private const string _correlationHeaderName = "X-Correlation-ID";
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ApiInfrastructureIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task Response_IncludesCorrelationIdHeader()
    {
        const string correlationId = "test-correlation-12345";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Add(_correlationHeaderName, correlationId);

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues(_correlationHeaderName, out var values).Should().BeTrue();
        values!.Single().Should().Be(correlationId);
    }

    [Fact]
    public async Task UnsupportedSortParameter_ReturnsBadRequestProblemDetails()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/content-types?sortBy=unsupportedField",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadProblemDetailsAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Bad Request");
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
        problem.TryGetProperty("correlationId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_ReturnsValidationProblemDetails()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/users",
            token,
            JsonContent.Create(new
            {
                email = "weak-password@contentforge.test",
                displayName = "Weak Password",
                password = "short",
                role = "Viewer",
            }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await ReadProblemDetailsAsync(response);
        problem.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.GetProperty("type").GetString().Should().Be("https://contentforge/errors/validation");
        problem.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListUsers_ReturnsPaginatedEnvelope()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/users?page=1&pageSize=10&sortBy=email",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.TryGetProperty("items", out _).Should().BeTrue();
        payload.GetProperty("page").GetInt32().Should().Be(1);
        payload.GetProperty("pageSize").GetInt32().Should().Be(10);
        payload.TryGetProperty("totalItems", out _).Should().BeTrue();
        payload.TryGetProperty("totalPages", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnsupportedFilterParameter_ReturnsBadRequestProblemDetails()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/content-types?authorId=123",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await ReadProblemDetailsAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Bad Request");
        problem.GetProperty("detail").GetString().Should().Contain("authorId");
    }

    [Fact]
    public async Task InvalidStatusFilter_ReturnsBadRequestProblemDetails()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/content?status=not-a-status",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnauthenticatedAdminEndpoint_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/content-types");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await ReadProblemDetailsAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Unauthorized");
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task IncludeDeleted_AsViewer_ReturnsForbidden()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/content?includeDeleted=true",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logout_WithoutUserId_Succeeds()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            token,
            JsonContent.Create(new { }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_ReturnsStableLoginResultDto()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = AuthTestConstants.AdminEmail,
                password = AuthTestConstants.AdminPassword,
            });
        login.EnsureSuccessStatusCode();
        var loginPayload = await login.Content.ReadFromJsonAsync<JsonElement>();

        var refresh = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = loginPayload.GetProperty("refreshToken").GetString() });

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("userId").ValueKind.Should().Be(JsonValueKind.String);
        payload.TryGetProperty("value", out _).Should().BeFalse();
        payload.EnumerateObject().Select(property => property.Name).Should().NotContain("passwordHash");
    }

    private static async Task<JsonElement> ReadProblemDetailsAsync(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        return document;
    }
}
