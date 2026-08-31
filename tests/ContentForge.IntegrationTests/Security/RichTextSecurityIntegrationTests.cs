namespace ContentForge.IntegrationTests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Api;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class RichTextSecurityIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentTypeApiScenario _contentTypeScenario = null!;
    private ContentApiScenario _contentScenario = null!;

    public RichTextSecurityIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
        _contentScenario = new ContentApiScenario(_client);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PublishedPublicContent_StripsRichTextXssSubmittedViaApi()
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
        var contentType = await _contentTypeScenario.CreateContentTypeAsync(adminToken, suffix);
        await _contentTypeScenario.AddFieldAsync(adminToken, contentType.Id, "title", FieldType.Text, 0, required: true);
        await _contentTypeScenario.AddFieldAsync(adminToken, contentType.Id, "body", FieldType.RichText, 1);

        const string maliciousBody = "<p>Safe intro</p><img src=x onerror=\"alert(1)\"><script>alert(1)</script>";
        var slug = $"xss-{suffix}";

        using var createRequest = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            authorToken,
            JsonContent.Create(new
            {
                contentTypeId = contentType.Id,
                slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = "XSS article",
                    ["body"] = maliciousBody,
                },
            }));

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var draft = (await createResponse.Content.ReadFromJsonAsync<ContentEntryDto>())!;
        draft.DraftData["body"]!.ToString()!.Should().NotContain("onerror");
        draft.DraftData["body"]!.ToString()!.Should().NotContain("<script");

        var published = await _contentScenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var publicResponse = await _client.GetAsync(
            $"/api/v1/public/{contentType.Slug}/{published.Slug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicPayload = await publicResponse.Content.ReadFromJsonAsync<PublicContentDto>();
        publicPayload.Should().NotBeNull();
        var body = publicPayload!.Data["body"]!.ToString()!;
        body.Should().Contain("Safe intro");
        body.Should().NotContain("onerror");
        body.Should().NotContain("<script");
    }
}
