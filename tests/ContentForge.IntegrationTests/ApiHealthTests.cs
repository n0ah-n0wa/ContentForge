using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace ContentForge.IntegrationTests;

public sealed class ApiHealthTests : IClassFixture<ContentForgeWebApplicationFactory>
{
    private const string _correlationHeaderName = "X-Correlation-ID";
    private readonly HttpClient _client;

    public ApiHealthTests(ContentForgeWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LiveHealthEndpoint_ReturnsHealthyWithoutDependencyChecks()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadHealthPayloadAsync(response);
        payload.GetProperty("status").GetString().Should().Be("Healthy");
        payload.TryGetProperty("checks", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ReadyHealthEndpoint_ReturnsHealthyWhenDependenciesAreAvailable()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadHealthPayloadAsync(response);
        payload.GetProperty("status").GetString().Should().Be("Healthy");
        payload.GetProperty("checks").GetProperty("database").GetString().Should().Be("Healthy");
        payload.GetProperty("checks").GetProperty("storage").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthEndpoints_DoNotExposeSecretsOrConnectionDetails()
    {
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        var combined = await live.Content.ReadAsStringAsync()
            + await ready.Content.ReadAsStringAsync();

        combined.Should().NotContain("Password=");
        combined.Should().NotContain("SigningKey");
        combined.Should().NotContain("ConnectionString");
        combined.Should().NotContain("contentforge_test");
        combined.Should().NotContain("Host=localhost");
    }

    [Fact]
    public async Task HealthEndpoints_IncludeCorrelationIdHeader()
    {
        const string correlationId = "health-correlation-abc123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(_correlationHeaderName, correlationId);

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues(_correlationHeaderName, out var values).Should().BeTrue();
        values!.Single().Should().Be(correlationId);
    }

    [Fact]
    public async Task HealthEndpoints_DoNotRequireAuthentication()
    {
        var live = await _client.GetAsync("/health/live");
        var ready = await _client.GetAsync("/health/ready");

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<JsonElement> ReadHealthPayloadAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        return payload;
    }
}
