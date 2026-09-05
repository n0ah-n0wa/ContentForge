namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Media.Models;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task UploadMedia_AsAdministrator_ReturnsCreatedAssetAndStoresBinaryOutsideDatabase()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        var response = await UploadAsync(token, "integration.txt", "text/plain", "integration-test-bytes"u8.ToArray(), "Alt text");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var asset = await response.Content.ReadFromJsonAsync<MediaAssetDto>();
        asset!.OriginalFileName.Should().Be("integration.txt");
        asset.FileName.Should().MatchRegex("^[0-9a-f]{32}\\.txt$");
        asset.AltText.Should().Be("Alt text");
        asset.Url.Should().StartWith("/media-files/media/");

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await dbContext.MediaAssets.SingleAsync(media => media.Id == asset.Id);
        entity.StorageKey.Should().StartWith("media/");
        entity.StorageKey.Should().NotContain("integration.txt");
        typeof(MediaAssetEntity).GetProperties().Should().NotContain(property =>
            property.PropertyType == typeof(byte[]) || property.PropertyType == typeof(byte?[]));

        var binaryPath = Path.Combine(_factory.MediaRoot, entity.StorageKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(binaryPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(binaryPath)).Should().Equal("integration-test-bytes"u8.ToArray());
    }

    [Fact]
    public async Task MediaLifecycle_UploadRetrieveUpdateDelete_WorksEndToEnd()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var createdResponse = await UploadAsync(token, "lifecycle.png", "image/png", [137, 80, 78, 71, 13, 10, 26, 10]);
        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createdResponse.Content.ReadFromJsonAsync<MediaAssetDto>();

        var getResponse = await _client.SendAsync(
            ApiTestHelper.CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/media/{created!.Id}", token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loaded = await getResponse.Content.ReadFromJsonAsync<MediaAssetDto>();
        loaded!.OriginalFileName.Should().Be("lifecycle.png");

        using var updateRequest = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/media/{created.Id}",
            token,
            JsonContent.Create(new { altText = "Updated alt", title = "Title", description = "Desc" }));
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MediaAssetDto>();
        updated!.AltText.Should().Be("Updated alt");
        updated.Title.Should().Be("Title");

        using var deleteRequest = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/media/{created.Id}",
            token);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var viewerToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);
        var getAfterDelete = await _client.SendAsync(
            ApiTestHelper.CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/media/{created.Id}", viewerToken));
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFile_AfterUpload_ReturnsBinaryWithoutAuthentication()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var payload = "public-media-bytes"u8.ToArray();
        var uploadResponse = await UploadAsync(token, "public.txt", "text/plain", payload);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var asset = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>();
        asset!.Url.Should().StartWith("/media-files/");

        var fileResponse = await _client.GetAsync(asset.Url);
        fileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        (await fileResponse.Content.ReadAsByteArrayAsync()).Should().Equal(payload);
    }

    [Fact]
    public async Task GetMediaFile_WhenSoftDeletedButBlobRemains_ReturnsNotFound()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var payload = "deleted-media-bytes"u8.ToArray();
        var uploadResponse = await UploadAsync(token, "deleted.txt", "text/plain", payload);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var asset = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.MediaAssets.SingleAsync(media => media.Id == asset!.Id);
            entity.IsDeleted = true;
            await dbContext.SaveChangesAsync();

            var binaryPath = Path.Combine(
                _factory.MediaRoot,
                entity.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(binaryPath).Should().BeTrue("blob must remain so the DB gate is under test");
        }

        var afterDelete = await _client.GetAsync(asset!.Url);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadMedia_WithPathTraversalFileName_Returns422()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var response = await UploadAsync(token, "../secrets.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithDisallowedExtension_Returns422()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var response = await UploadAsync(token, "payload.exe", "application/octet-stream", [0x4D, 0x5A]);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_AsViewer_ReturnsForbidden()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        var response = await UploadAsync(token, "viewer.txt", "text/plain", "nope"u8.ToArray());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private async Task<HttpResponseMessage> UploadAsync(
        string token,
        string fileName,
        string contentType,
        byte[] bytes,
        string? altText = null)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        if (altText is not null)
        {
            content.Add(new StringContent(altText), "altText");
        }

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/media",
            token,
            content);

        return await _client.SendAsync(request);
    }
}
