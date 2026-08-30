namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Auth;
using FluentAssertions;

internal sealed class ContentTypeApiScenario
{
    private readonly HttpClient _client;

    internal ContentTypeApiScenario(HttpClient client)
    {
        _client = client;
    }

    internal async Task<(string AdminToken, string AuthorToken, string EditorToken, string ViewerToken)> LoginAllAsync()
    {
        var adminToken = await ApiTestHelper.LoginAsync(_client, AuthTestConstants.AdminEmail, AuthTestConstants.AdminPassword);
        var authorToken = await ApiTestHelper.LoginAsync(_client, AuthTestConstants.AuthorEmail, AuthTestConstants.AuthorPassword);
        var editorToken = await ApiTestHelper.LoginAsync(_client, AuthTestConstants.EditorEmail, AuthTestConstants.EditorPassword);
        var viewerToken = await ApiTestHelper.LoginAsync(_client, AuthTestConstants.ViewerEmail, AuthTestConstants.ViewerPassword);
        return (adminToken, authorToken, editorToken, viewerToken);
    }

    internal async Task<ContentTypeDto> CreateContentTypeAsync(
        string token,
        string? suffix = null,
        string? name = null,
        string? slug = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/content-types",
            token,
            new
            {
                name = name ?? $"type{suffix}",
                displayName = "Dynamic Type",
                slug = slug ?? $"dynamic-{suffix}",
                description = "Integration test content type",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentTypeDto>())!;
    }

    internal async Task<ContentTypeDto> AddFieldAsync(
        string token,
        Guid contentTypeId,
        string name,
        FieldType fieldType,
        int sortOrder,
        bool required = false,
        IReadOnlyList<string>? options = null,
        Guid? relationTarget = null,
        RelationCardinality? relationCardinality = null,
        bool allowMultiple = false)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentTypeId}/fields",
            token,
            new
            {
                name,
                fieldType,
                displayName = name,
                sortOrder,
                configuration = BuildConfiguration(required, options, relationTarget, relationCardinality, allowMultiple),
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentTypeDto>())!;
    }

    internal static object BuildConfiguration(
        bool required,
        IReadOnlyList<string>? options = null,
        Guid? relationTarget = null,
        RelationCardinality? relationCardinality = null,
        bool allowMultiple = false) =>
        new
        {
            isRequired = required,
            minLength = (int?)null,
            maxLength = (int?)null,
            minValue = (decimal?)null,
            maxValue = (decimal?)null,
            pattern = (string?)null,
            allowMultiple,
            defaultValue = (string?)null,
            options = options ?? Array.Empty<string>(),
            relationTarget,
            relationCardinality,
        };

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
}
