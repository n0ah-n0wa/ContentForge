namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Dashboard.Models;
using ContentForge.Domain.Content;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class DashboardApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public DashboardApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new ContentForgeWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
        _scenario = new ContentApiScenario(_client);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Get_Admin_ReturnsBackendDerivedStatisticsAndRecentContent()
    {
        var (contentType, adminToken, authorToken, _) = await _scenario.SeedAsync();
        var slug = $"dashboard-{Guid.NewGuid():N}";
        await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);

        var response = await _scenario.SendAsync(HttpMethod.Get, "/api/v1/dashboard", adminToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dashboard = (await response.Content.ReadFromJsonAsync<DashboardDto>())!;
        dashboard.ContentStatistics.Should().NotBeNull();
        dashboard.ContentStatistics!.TotalContent.Should().BeGreaterThan(0);
        dashboard.ContentStatistics.DraftCount.Should().BeGreaterThan(0);
        dashboard.RecentContent.Should().Contain(item => item.Slug == slug);
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
