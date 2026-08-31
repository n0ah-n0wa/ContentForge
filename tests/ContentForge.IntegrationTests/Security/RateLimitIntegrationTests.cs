namespace ContentForge.IntegrationTests.Security;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Auth.Models;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[Collection(PersistenceTests.Name)]
public sealed class RateLimitIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private LowRateLimitWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public RateLimitIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new LowRateLimitWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_ExceedingLimit_Returns429WithProblemDetails()
    {
        var lastResponse = await BurstAsync(
            attempt => _client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(AuthTestConstants.ViewerEmail, $"wrong-{attempt}")),
            expectedLimit: 3);

        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.RetryAfter.Should().NotBeNull();
        var problem = await lastResponse.Content.ReadAsStringAsync();
        problem.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task Refresh_ExceedingLimit_Returns429()
    {
        var lastResponse = await BurstAsync(
            _ => _client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest("invalid-refresh-token")),
            expectedLimit: 3);

        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ForgotPassword_ExceedingLimit_Returns429()
    {
        var lastResponse = await BurstAsync(
            _ => _client.PostAsJsonAsync(
                "/api/v1/auth/forgot-password",
                new ForgotPasswordRequest(AuthTestConstants.ViewerEmail)),
            expectedLimit: 3);

        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task PublicApi_ExceedingLimit_Returns429()
    {
        var lastResponse = await BurstAsync(
            _ => _client.GetAsync("/api/v1/public/non-existent-type"),
            expectedLimit: 3);

        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ContentPreview_ExceedingLimit_Returns429()
    {
        var lastResponse = await BurstAsync(
            attempt => _client.GetAsync($"/api/v1/content/preview/invalid-token-{attempt}"),
            expectedLimit: 3);

        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task AuthenticatedAdminRequests_AreNotRateLimitedByAbusePolicies()
    {
        var token = await LoginAsync(_client, AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    private static async Task<HttpResponseMessage> BurstAsync(
        Func<int, Task<HttpResponseMessage>> send,
        int expectedLimit)
    {
        HttpResponseMessage? lastResponse = null;
        for (var attempt = 0; attempt < expectedLimit + 1; attempt++)
        {
            lastResponse = await send(attempt);
        }

        return lastResponse!;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        return payload!.AccessToken;
    }

    private sealed class LowRateLimitWebApplicationFactory : ContentForgeWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:AuthLogin:PermitLimit"] = "3",
                    ["RateLimiting:AuthRefresh:PermitLimit"] = "3",
                    ["RateLimiting:PasswordResetRequest:PermitLimit"] = "3",
                    ["RateLimiting:PublicApi:PermitLimit"] = "3",
                    ["RateLimiting:ContentPreview:PermitLimit"] = "3",
                });
            });
        }
    }
}
