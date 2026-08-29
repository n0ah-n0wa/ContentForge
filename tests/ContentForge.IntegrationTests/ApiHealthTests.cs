using System.Net;
using FluentAssertions;

namespace ContentForge.IntegrationTests;

public sealed class ApiHealthTests : IClassFixture<ContentForgeWebApplicationFactory>
{
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
}
