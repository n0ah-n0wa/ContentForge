namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Media.Models;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class MediaApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public MediaApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task UploadMedia_AsAdministrator_ReturnsCreatedAsset()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("integration-test-bytes"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "integration.txt");
        content.Add(new StringContent("Alt text"), "altText");

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/media",
            token,
            content);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var asset = await response.Content.ReadFromJsonAsync<MediaAssetDto>();
        asset!.OriginalFileName.Should().Be("integration.txt");
        asset.AltText.Should().Be("Alt text");
    }

    [Fact]
    public async Task CreateUser_AsViewer_ReturnsForbidden()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/users",
            token,
            JsonContent.Create(new
            {
                email = "blocked-viewer@contentforge.test",
                displayName = "Blocked Viewer",
                password = "SecurePassword123!",
                role = "Viewer",
            }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
