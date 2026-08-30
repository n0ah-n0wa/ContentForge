namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ObservabilityIntegrationTests : IAsyncLifetime
{
    private const string _correlationHeaderName = "X-Correlation-ID";
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ObservabilityIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task ErrorResponse_IncludesProvidedCorrelationId()
    {
        const string correlationId = "observability-correlation-001";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/content-types?sortBy=unsupportedField");
        request.Headers.Add(_correlationHeaderName, correlationId);

        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues(_correlationHeaderName, out var headerValues).Should().BeTrue();
        headerValues!.Single().Should().Be(correlationId);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("correlationId").GetString().Should().Be(correlationId);
        problem.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ErrorResponse_IncludesGeneratedCorrelationIdWhenHeaderMissing()
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/content-types?sortBy=unsupportedField",
            await ApiTestHelper.LoginAsync(
                _client,
                AuthTestConstants.AdminEmail,
                AuthTestConstants.AdminPassword));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues(_correlationHeaderName, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();
        correlationId.Should().NotBeNullOrWhiteSpace();

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("correlationId").GetString().Should().Be(correlationId);
    }

    [Fact]
    public async Task UnauthorizedResponse_IncludesTraceAndCorrelationIdentifiers()
    {
        var response = await _client.GetAsync("/api/v1/content-types");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues(_correlationHeaderName, out _).Should().BeTrue();

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
        problem.TryGetProperty("correlationId", out _).Should().BeTrue();
    }
}
