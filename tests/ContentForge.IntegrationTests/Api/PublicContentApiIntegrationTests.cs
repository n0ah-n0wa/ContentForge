namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Content;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class PublicContentApiIntegrationTests : IAsyncLifetime
{
    private static readonly string[] _leakedPropertyNames =
    [
        "id",
        "contentTypeId",
        "status",
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
    ];

    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public PublicContentApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task GetBySlug_Draft_Returns404()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"draft-{Guid.NewGuid():N}");

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{draft.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_InReview_Returns404()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"review-{Guid.NewGuid():N}");
        await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{draft.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Archived_Returns404()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"archived-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var archiveResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/archive",
            editorToken,
            new { changeSummary = "Archive", concurrencyToken = published.ConcurrencyToken });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{published.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlug_Unpublished_Returns404()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"unpub-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var unpublishResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/unpublish",
            editorToken,
            new { changeSummary = "Unpublish", concurrencyToken = published.ConcurrencyToken });
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{published.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ExcludesUnpublishedStatuses()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var publishedDraft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"pub-{Guid.NewGuid():N}", "Published title");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, publishedDraft);

        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"hidden-draft-{Guid.NewGuid():N}");
        var review = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"hidden-review-{Guid.NewGuid():N}");
        await _scenario.SubmitForReviewAsync(authorToken, review.Id, review.ConcurrencyToken);

        var archivedDraft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"hidden-arch-{Guid.NewGuid():N}");
        var archived = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, archivedDraft);
        var archiveResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{archived.Id}/archive",
            editorToken,
            new { changeSummary = "Archive", concurrencyToken = archived.ConcurrencyToken });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedResult<PublicContentDto>>();
        payload!.Items.Should().ContainSingle(item => item.Slug == published.Slug);
        payload.Items.Should().NotContain(item => item.Slug == draft.Slug);
        payload.Items.Should().NotContain(item => item.Slug == review.Slug);
        payload.Items.Should().NotContain(item => item.Slug == archived.Slug);
        payload.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetBySlug_Published_ReturnsStablePublicDtoWithoutInternalFields()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"stable-{Guid.NewGuid():N}", "Public title");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{draft.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["contentTypeSlug", "slug", "data", "publishedAt"]);
        json.GetProperty("contentTypeSlug").GetString().Should().Be(contentType.Slug);
        json.GetProperty("slug").GetString().Should().Be(draft.Slug);
        json.GetProperty("data").GetProperty("title").GetString().Should().Be("Public title");
        AssertNoLeakedProperties(json);
    }

    [Fact]
    public async Task List_SupportsPaginationAndSlugFilter()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var first = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"alpha{Guid.NewGuid():N}"[..18], "Alpha");
        var second = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"beta{Guid.NewGuid():N}"[..18], "Beta");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, first);
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, second);

        var pageResponse = await _client.GetAsync($"/api/v1/public/{contentType.Slug}?page=1&pageSize=1&sortBy=slug&sortDirection=asc");
        pageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await pageResponse.Content.ReadFromJsonAsync<PaginatedResult<PublicContentDto>>();
        page!.Items.Should().HaveCount(1);
        page.PageSize.Should().Be(1);
        page.TotalItems.Should().Be(2);
        page.TotalPages.Should().Be(2);

        var filtered = await _client.GetAsync($"/api/v1/public/{contentType.Slug}?slug={second.Slug}");
        filtered.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredPage = await filtered.Content.ReadFromJsonAsync<PaginatedResult<PublicContentDto>>();
        filteredPage!.Items.Should().ContainSingle();
        filteredPage.Items[0].Slug.Should().Be(second.Slug);
    }

    [Fact]
    public async Task List_StatusFilter_IsRejected()
    {
        var (contentType, _, _, _) = await _scenario.SeedAsync();

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}?status=draft");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenApi_DocumentsPublicContentContract()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        document.GetProperty("info").GetProperty("description").GetString().Should().Contain("published snapshots only");

        var paths = document.GetProperty("paths");
        paths.TryGetProperty("/api/v1/public/{contentTypeSlug}", out var collection).Should().BeTrue();
        paths.TryGetProperty("/api/v1/public/{contentTypeSlug}/{slug}", out var item).Should().BeTrue();
        collection.GetProperty("get").GetProperty("tags")[0].GetString().Should().Be("Public Content");
        item.GetProperty("get").GetProperty("summary").GetString().Should().Contain("published");

        var schema = document.GetProperty("components").GetProperty("schemas").GetProperty("PublicContentDto");
        var propertyNames = schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToArray();
        propertyNames.Should().BeEquivalentTo(["contentTypeSlug", "slug", "data", "publishedAt"]);
        propertyNames.Should().NotIntersectWith(_leakedPropertyNames);
    }

    [Fact]
    public async Task PublicContent_DoesNotRequireAuthentication()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"anon-{Guid.NewGuid():N}");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/public/{contentType.Slug}/{draft.Slug}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RequestMessage!.Headers.Authorization.Should().BeNull();
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
