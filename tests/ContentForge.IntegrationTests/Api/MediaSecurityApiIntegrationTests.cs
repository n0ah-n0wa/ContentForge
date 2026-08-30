namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.Media.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.Domain.Media;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class MediaSecurityApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentTypeApiScenario _contentTypeScenario = null!;

    public MediaSecurityApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new ContentForgeWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
        _contentTypeScenario = new ContentTypeApiScenario(_client);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UploadMedia_WithSpoofedExecutableContent_Returns422()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "payload.jpg", "image/jpeg", [0x4D, 0x5A, 0x90, 0x00]);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithDoubleExtensionAndExecutableContent_Returns422()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "payload.exe.jpg", "image/jpeg", [0x4D, 0x5A, 0x90, 0x00]);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithSvgScriptPayload_Returns422()
    {
        var token = await LoginEditorAsync();
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();
        var response = await UploadAsync(token, "icon.svg", "image/svg+xml", svg);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithUnsafeNullByteFileName_Returns422()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "cover\0.jpg", "image/jpeg", ValidJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithAlternateDataStreamFileName_Returns422()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "cover.jpg:secret", "image/jpeg", ValidJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithEmptyFile_Returns422()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "empty.jpg", "image/jpeg", []);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithOversizedFile_Returns422()
    {
        var token = await LoginEditorAsync();
        var oversized = new byte[MediaUploadRules.MaxFileSizeBytes + 1];
        oversized[0] = 0xFF;
        oversized[1] = 0xD8;
        oversized[2] = 0xFF;

        var response = await UploadAsync(token, "huge.jpg", "image/jpeg", oversized);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_WithoutMultipartBody_Returns415()
    {
        var token = await LoginEditorAsync();

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/media",
            token,
            JsonContent.Create(new { fileName = "photo.jpg" }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task UploadMedia_WithMissingFilePart_Returns422()
    {
        var token = await LoginEditorAsync();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Alt text"), "altText");

        using var request = ApiTestHelper.CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/media", token, content);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UploadMedia_AsViewer_ReturnsForbidden()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        var response = await UploadAsync(token, "viewer.txt", "text/plain", "blocked"u8.ToArray());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteMedia_AsAuthorWithoutDeletePermission_ReturnsForbidden()
    {
        var editorToken = await LoginEditorAsync();
        var created = await UploadAsync(editorToken, "delete-me.txt", "text/plain", "delete-me"u8.ToArray());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var asset = await created.Content.ReadFromJsonAsync<MediaAssetDto>();

        var authorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AuthorEmail,
            AuthTestConstants.AuthorPassword);

        using var deleteRequest = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/media/{asset!.Id}",
            authorToken);

        var response = await _client.SendAsync(deleteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateContent_WithNonExistentMediaReference_Returns422()
    {
        var (adminToken, authorToken, _, _) = await _contentTypeScenario.LoginAllAsync();
        var contentType = await CreateContentTypeWithMediaFieldAsync(adminToken);
        var missingMediaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            authorToken,
            JsonContent.Create(new
            {
                contentTypeId = contentType.Id,
                slug = "missing-media-ref",
                data = new Dictionary<string, object?>
                {
                    ["title"] = "Title",
                    ["heroImage"] = missingMediaId,
                },
            }));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors").EnumerateObject()
            .SelectMany(property => property.Value.EnumerateArray().Select(item => item.GetString()))
            .Where(message => message is not null)
            .ToArray();
        errors.Should().Contain(message => message!.Contains("do not exist or have been deleted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PublishContent_WithDeletedMediaReference_Returns422()
    {
        var (adminToken, authorToken, editorToken, _) = await _contentTypeScenario.LoginAllAsync();
        var contentType = await CreateContentTypeWithMediaFieldAsync(adminToken);

        var uploadResponse = await UploadAsync(editorToken, "hero.png", "image/png", ValidPng());
        var media = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>();

        var entry = await CreateDraftWithMediaAsync(authorToken, contentType, media!.Id, "deleted-media-ref");
        var submitted = await SubmitForReviewAsync(authorToken, entry);

        using var deleteRequest = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/media/{media.Id}",
            editorToken);
        (await _client.SendAsync(deleteRequest)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publishResponse = await PublishAsync(editorToken, submitted, "publish with deleted media");

        publishResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PublicContent_DoesNotExposeAdministrativeMetadata()
    {
        var (adminToken, authorToken, editorToken, _) = await _contentTypeScenario.LoginAllAsync();
        var contentType = await CreateContentTypeWithMediaFieldAsync(adminToken);

        var uploadResponse = await UploadAsync(editorToken, "public-hero.png", "image/png", ValidPng());
        var media = await uploadResponse.Content.ReadFromJsonAsync<MediaAssetDto>();

        var entry = await CreateDraftWithMediaAsync(authorToken, contentType, media!.Id, "public-media-entry");
        var submitted = await SubmitForReviewAsync(authorToken, entry);
        var published = await PublishAsync(editorToken, submitted, "publish media entry");
        published.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/public-media-entry");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await publicResponse.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("uploadedBy", because: "public responses must not expose uploader identity");
        rawJson.Should().NotContain("storageKey", because: "public responses must not expose storage paths");
        rawJson.Should().NotContain("isDeleted", because: "public responses must not expose soft-delete state");
        rawJson.Should().NotContain("originalFileName", because: "public responses must not expose upload filenames");

        var publicItem = await publicResponse.Content.ReadFromJsonAsync<PublicContentDto>();
        publicItem!.Data["heroImage"]!.ToString().Should().Be(media.Id.ToString());
    }

    [Fact]
    public async Task UploadMedia_ResponseDoesNotExposeStorageKey()
    {
        var token = await LoginEditorAsync();
        var response = await UploadAsync(token, "metadata.txt", "text/plain", "metadata-check"u8.ToArray());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("storageKey");
    }

    private async Task<string> LoginEditorAsync() =>
        await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

    private async Task<ContentTypeDto> CreateContentTypeWithMediaFieldAsync(string adminToken)
    {
        var contentType = await _contentTypeScenario.CreateContentTypeAsync(adminToken);
        await _contentTypeScenario.AddFieldAsync(adminToken, contentType.Id, "title", FieldType.Text, sortOrder: 0, required: true);
        await _contentTypeScenario.AddFieldAsync(adminToken, contentType.Id, "heroImage", FieldType.Media, sortOrder: 1, required: false);
        return contentType;
    }

    private async Task<ContentEntryDto> CreateDraftWithMediaAsync(
        string token,
        ContentTypeDto contentType,
        Guid mediaId,
        string slug)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            token,
            JsonContent.Create(new
            {
                contentTypeId = contentType.Id,
                slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = "Title",
                    ["heroImage"] = mediaId,
                },
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> SubmitForReviewAsync(string token, ContentEntryDto entry)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/submit-for-review",
            token,
            JsonContent.Create(new { concurrencyToken = entry.ConcurrencyToken }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<HttpResponseMessage> PublishAsync(
        string token,
        ContentEntryDto entry,
        string changeSummary)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/publish",
            token,
            JsonContent.Create(new { changeSummary, concurrencyToken = entry.ConcurrencyToken }));

        return await _client.SendAsync(request);
    }

    private static byte[] ValidJpeg() => [0xFF, 0xD8, 0xFF, 0xD9];

    private static byte[] ValidPng() => [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0];

    private async Task<HttpResponseMessage> UploadAsync(
        string token,
        string fileName,
        string contentType,
        byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/media",
            token,
            content);

        return await _client.SendAsync(request);
    }
}
