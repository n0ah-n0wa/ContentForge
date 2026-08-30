namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class ContentWorkflowApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ContentWorkflowApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task ContentWorkflow_CreatePublishAndReadPublicEndpoint()
    {
        var adminToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        var contentType = await CreateContentTypeAsync(adminToken);
        await AddTitleFieldAsync(adminToken, contentType.Id);
        await AddBodyFieldAsync(adminToken, contentType.Id);

        var authorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AuthorEmail,
            AuthTestConstants.AuthorPassword);

        var entry = await CreateContentEntryAsync(authorToken, contentType.Id, "hello-world");

        var draftPublicResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/hello-world");
        draftPublicResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var submitted = await SubmitForReviewAsync(authorToken, entry.Id, entry.ConcurrencyToken);

        var editorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var published = await PublishContentAsync(editorToken, submitted.Id, submitted.ConcurrencyToken);
        published.Status.Should().Be(Domain.Content.ContentStatus.Published);

        var publicListResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}?page=1&pageSize=10");
        publicListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicList = await publicListResponse.Content.ReadFromJsonAsync<JsonElement>();
        publicList.GetProperty("items").GetArrayLength().Should().Be(1);

        var publicItemResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/hello-world");
        publicItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicItem = await publicItemResponse.Content.ReadFromJsonAsync<PublicContentDto>();
        publicItem!.Slug.Should().Be("hello-world");
        publicItem.Data["title"]!.ToString().Should().Be("Hello World");
    }

    [Fact]
    public async Task PublicContent_DoesNotRequireAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/public/articles?page=1&pageSize=10");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListAuditLogs_AsViewer_ReturnsForbidden()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/audit",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListAuditLogs_AsAdministrator_ReturnsPaginatedResults()
    {
        var token = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            "/api/v1/audit?page=1&pageSize=20&sortBy=timestamp&sortDirection=desc",
            token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.TryGetProperty("items", out _).Should().BeTrue();
    }

    private async Task<ContentTypeDto> CreateContentTypeAsync(string adminToken)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content-types",
            adminToken,
            JsonContent.Create(new
            {
                name = "article",
                displayName = "Article",
                slug = "articles",
                description = "Integration test articles",
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentTypeDto>())!;
    }

    private async Task AddTitleFieldAsync(string adminToken, Guid contentTypeId)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentTypeId}/fields",
            adminToken,
            JsonContent.Create(new
            {
                name = "title",
                fieldType = FieldType.Text,
                displayName = "Title",
                sortOrder = 1,
                configuration = new
                {
                    isRequired = true,
                    minLength = (int?)null,
                    maxLength = 200,
                    minValue = (decimal?)null,
                    maxValue = (decimal?)null,
                    pattern = (string?)null,
                    allowMultiple = false,
                    defaultValue = (string?)null,
                    options = Array.Empty<string>(),
                    relationTarget = (Guid?)null,
                    relationCardinality = (RelationCardinality?)null,
                },
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddBodyFieldAsync(string adminToken, Guid contentTypeId)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentTypeId}/fields",
            adminToken,
            JsonContent.Create(new
            {
                name = "body",
                fieldType = FieldType.LongText,
                displayName = "Body",
                sortOrder = 2,
                configuration = new
                {
                    isRequired = false,
                    minLength = (int?)null,
                    maxLength = (int?)null,
                    minValue = (decimal?)null,
                    maxValue = (decimal?)null,
                    pattern = (string?)null,
                    allowMultiple = false,
                    defaultValue = (string?)null,
                    options = Array.Empty<string>(),
                    relationTarget = (Guid?)null,
                    relationCardinality = (RelationCardinality?)null,
                },
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<ContentEntryDto> CreateContentEntryAsync(string authorToken, Guid contentTypeId, string slug)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            authorToken,
            JsonContent.Create(new
            {
                contentTypeId,
                slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = "Hello World",
                    ["body"] = "Draft body",
                },
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> SubmitForReviewAsync(string authorToken, Guid entryId, uint concurrencyToken)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/submit-for-review",
            authorToken,
            JsonContent.Create(new { concurrencyToken }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> PublishContentAsync(string editorToken, Guid entryId, uint concurrencyToken)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/publish",
            editorToken,
            JsonContent.Create(new
            {
                changeSummary = "Initial publish",
                concurrencyToken,
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }
}
