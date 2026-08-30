namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Auth;
using FluentAssertions;

internal sealed class ContentApiScenario
{
    private readonly HttpClient _client;

    internal ContentApiScenario(HttpClient client)
    {
        _client = client;
    }

    internal async Task<(ContentTypeDto ContentType, string AdminToken, string AuthorToken, string EditorToken)> SeedAsync()
    {
        var adminToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);
        var authorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AuthorEmail,
            AuthTestConstants.AuthorPassword);
        var editorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var contentType = await CreateContentTypeAsync(adminToken, suffix);
        await AddFieldAsync(adminToken, contentType.Id, "title", FieldType.Text, required: true);
        await AddFieldAsync(adminToken, contentType.Id, "body", FieldType.LongText, required: false);

        return (contentType, adminToken, authorToken, editorToken);
    }

    internal async Task<ContentEntryDto> CreateDraftAsync(
        string token,
        Guid contentTypeId,
        string slug,
        string title = "Draft title",
        string body = "Draft body")
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            token,
            JsonContent.Create(new
            {
                contentTypeId,
                slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = title,
                    ["body"] = body,
                },
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    internal async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string? token = null,
        object? body = null)
    {
        HttpContent? content = body is null ? null : JsonContent.Create(body);
        if (token is null)
        {
            return body is null
                ? await _client.SendAsync(new HttpRequestMessage(method, url))
                : await _client.SendAsync(new HttpRequestMessage(method, url) { Content = content });
        }

        using var request = ApiTestHelper.CreateAuthenticatedRequest(method, url, token, content);
        return await _client.SendAsync(request);
    }

    internal async Task<ContentEntryDto> SubmitForReviewAsync(string token, Guid entryId, uint concurrencyToken)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/submit-for-review",
            token,
            new { concurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    internal async Task<ContentEntryDto> PublishAsync(
        string token,
        Guid entryId,
        uint concurrencyToken,
        string changeSummary = "Published")
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/publish",
            token,
            new { changeSummary, concurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    internal async Task<ContentEntryDto> AdvanceToPublishedAsync(
        string authorToken,
        string editorToken,
        ContentEntryDto draft)
    {
        var submitted = await SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);
        return await PublishAsync(editorToken, submitted.Id, submitted.ConcurrencyToken);
    }

    internal async Task<ContentEntryDto> UnpublishAsync(
        string token,
        Guid entryId,
        uint concurrencyToken,
        string changeSummary = "Unpublished")
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/unpublish",
            token,
            new { changeSummary, concurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    internal async Task<ContentEntryDto> ArchiveAsync(
        string token,
        Guid entryId,
        uint concurrencyToken,
        string changeSummary = "Archived")
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/archive",
            token,
            new { changeSummary, concurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    internal Task AddFieldAsync(
        string adminToken,
        Guid contentTypeId,
        string name,
        FieldType fieldType,
        bool required) =>
        AddFieldInternalAsync(adminToken, contentTypeId, name, fieldType, required);

    private async Task<ContentTypeDto> CreateContentTypeAsync(string adminToken, string suffix)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content-types",
            adminToken,
            JsonContent.Create(new
            {
                name = $"article{suffix}",
                displayName = "Article",
                slug = $"articles-{suffix}",
                description = "Content API integration test type",
            }));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentTypeDto>())!;
    }

    private async Task AddFieldInternalAsync(
        string adminToken,
        Guid contentTypeId,
        string name,
        FieldType fieldType,
        bool required)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentTypeId}/fields",
            adminToken,
            JsonContent.Create(new
            {
                name,
                fieldType,
                displayName = name switch { "title" => "Title", "body" => "Body", _ => name },
                sortOrder = name == "title" ? 1 : 2,
                configuration = new
                {
                    isRequired = required,
                    minLength = (int?)null,
                    maxLength = name == "title" ? 200 : (int?)null,
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
}
