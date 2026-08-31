namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Domain.Content;
using ContentForge.Infrastructure.Persistence;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(PersistenceTests.Name)]
public sealed class ContentPreviewSecurityIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] _leakedPropertyNames =
    [
        "id",
        "contentTypeId",
        "draftData",
        "publishedData",
        "currentVersion",
        "concurrencyToken",
        "createdBy",
        "updatedBy",
        "createdAt",
        "updatedAt",
        "publishedBy",
        "isDeleted",
        "versions",
        "userId",
        "email",
        "role",
        "token",
    ];

    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ContentPreviewSecurityIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task Preview_AllowsAuthorizedEditorToViewDraftWithoutAdminCredentials()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"preview-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Preview headline");

        var publicResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var tokenResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = await tokenResponse.Content.ReadFromJsonAsync<ContentPreviewTokenDto>();
        issued!.Token.Should().NotBeNullOrWhiteSpace();
        issued.PreviewPath.Should().Contain("/api/v1/content/preview/");

        using var previewRequest = new HttpRequestMessage(HttpMethod.Get, issued.PreviewPath);
        var previewResponse = await _client.SendAsync(previewRequest);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        previewResponse.RequestMessage!.Headers.Authorization.Should().BeNull();

        var body = await previewResponse.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        var preview = JsonSerializer.Deserialize<ContentPreviewDto>(body, _jsonOptions);
        preview!.ContentTypeSlug.Should().Be(contentType.Slug);
        preview.Slug.Should().Be(slug);
        preview.Status.Should().Be(ContentStatus.Draft);
        preview.Data["title"]!.ToString().Should().Be("Preview headline");
        preview.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        AssertNoLeakedProperties(json);
    }

    [Fact]
    public async Task Preview_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/content/preview/not-a-valid-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preview_IssuedToken_IsOpaqueAndDoesNotExposeAdminCredentials()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"opaque-{Guid.NewGuid():N}");

        var tokenResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var issued = (await tokenResponse.Content.ReadFromJsonAsync<ContentPreviewTokenDto>())!;
        issued.Token.Split('.').Should().NotHaveCount(3, "preview tokens must not be JWT access tokens");
        issued.Token.Should().NotContain("@");
        issued.PreviewPath.Should().EndWith(issued.Token);
    }

    [Fact]
    public async Task Preview_AfterExpiration_Returns401()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"expired-{Guid.NewGuid():N}");

        var tokenResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);
        var issued = (await tokenResponse.Content.ReadFromJsonAsync<ContentPreviewTokenDto>())!;

        await ExpirePreviewTokenAsync(issued.Token);

        var response = await _client.GetAsync(issued.PreviewPath);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preview_AfterRevocation_Returns401()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"revoked-{Guid.NewGuid():N}");

        var firstTokenResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);
        var firstToken = (await firstTokenResponse.Content.ReadFromJsonAsync<ContentPreviewTokenDto>())!;

        await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);

        var response = await _client.GetAsync(firstToken.PreviewPath);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preview_CreateToken_ForbiddenForAuthorOnForeignContent()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(editorToken, contentType.Id, $"foreign-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            authorToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Preview_CreateToken_RequiresAuthentication()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"auth-{Guid.NewGuid():N}");

        var response = await _client.PostAsync($"/api/v1/content/{draft.Id}/preview-token", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preview_DraftRemainsUnavailableOnPublicApi()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"still-hidden-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Still hidden");

        var tokenResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/preview-token",
            editorToken);
        var issued = (await tokenResponse.Content.ReadFromJsonAsync<ContentPreviewTokenDto>())!;

        var previewResponse = await _client.GetAsync(issued.PreviewPath);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task ExpirePreviewTokenAsync(string token)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenHash = ContentForge.Infrastructure.Identity.JwtTokenService.HashToken(token);

        await dbContext.ContentPreviewTokens
            .Where(previewToken => previewToken.TokenHash == tokenHash)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(entity => entity.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-5)));
    }

    private static void AssertNoLeakedProperties(JsonElement json)
    {
        var names = CollectPropertyNames(json);
        names.Should().NotIntersectWith(_leakedPropertyNames);
    }

    private static HashSet<string> CollectPropertyNames(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        CollectPropertyNames(element, names);
        return names;
    }

    private static void CollectPropertyNames(JsonElement element, ISet<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    names.Add(property.Name);
                    CollectPropertyNames(property.Value, names);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectPropertyNames(item, names);
                }

                break;
        }
    }
}
