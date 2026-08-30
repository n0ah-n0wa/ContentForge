namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Content;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ContentApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ContentApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task Create_ValidDraft_Returns201WithEntry()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var slug = $"create-{Guid.NewGuid():N}";

        var entry = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);

        entry.Slug.Should().Be(slug);
        entry.Status.Should().Be(ContentStatus.Draft);
        entry.ContentTypeId.Should().Be(contentType.Id);
    }

    [Fact]
    public async Task Get_ExistingEntry_Returns200()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"get-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{created.Id}", authorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Update_ValidDraft_Returns200AndIncrementsVersion()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"update-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{created.Id}",
            authorToken,
            new
            {
                slug = created.Slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = "Updated title",
                    ["body"] = "Updated body",
                },
                changeSummary = "Updated draft",
                concurrencyToken = created.ConcurrencyToken,
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        updated!.ConcurrencyToken.Should().BeGreaterThan(created.ConcurrencyToken);
        updated.DraftData["title"]!.ToString().Should().Be("Updated title");
    }

    [Fact]
    public async Task Delete_SoftDeletesEntry_Returns204()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"delete-{Guid.NewGuid():N}");

        var deleteResponse = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content/{created.Id}",
            editorToken,
            new { concurrencyToken = created.ConcurrencyToken });

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{created.Id}", editorToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ReturnsPaginatedResultsWithFiltersAndSorting()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"list-{Guid.NewGuid():N}";
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content?contentTypeId={contentType.Id}&status=Published&page=1&pageSize=10&sortBy=slug&sortDirection=asc",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        payload.GetProperty("page").GetInt32().Should().Be(1);
        payload.GetProperty("totalItems").GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Search_ReturnsMatchingEntry()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var uniqueTitle = $"SearchTarget-{Guid.NewGuid():N}";
        var created = await _scenario.CreateDraftAsync(
            authorToken,
            contentType.Id,
            $"search-{Guid.NewGuid():N}",
            title: uniqueTitle);
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/search?keyword={Uri.EscapeDataString(uniqueTitle)}&contentTypeId={contentType.Id}",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitForReview_FromDraft_Returns200()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"submit-{Guid.NewGuid():N}");

        var submitted = await _scenario.SubmitForReviewAsync(authorToken, created.Id, created.ConcurrencyToken);

        submitted.Status.Should().Be(ContentStatus.InReview);
    }

    [Fact]
    public async Task WithdrawFromReview_FromInReview_Returns200()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"withdraw-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, created.Id, created.ConcurrencyToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/withdraw-from-review",
            authorToken,
            new { concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task Publish_FromInReview_Returns200()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"publish-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, created.Id, created.ConcurrencyToken);

        var published = await _scenario.PublishAsync(editorToken, submitted.Id, submitted.ConcurrencyToken);

        published.Status.Should().Be(ContentStatus.Published);
        published.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Unpublish_FromPublished_Returns200()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"unpublish-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/unpublish",
            editorToken,
            new { changeSummary = "Unpublish", concurrencyToken = published.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task Archive_FromPublished_Returns200()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"archive-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/archive",
            editorToken,
            new { changeSummary = "Archive", concurrencyToken = published.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.Status.Should().Be(ContentStatus.Archived);
    }

    [Fact]
    public async Task Restore_FromArchived_Returns200()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"restore-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var archivedResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/archive",
            editorToken,
            new { changeSummary = "Archive", concurrencyToken = published.ConcurrencyToken });
        var archived = await archivedResponse.Content.ReadFromJsonAsync<ContentEntryDto>();

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{archived!.Id}/restore",
            editorToken,
            new { changeSummary = "Restore archived", concurrencyToken = archived.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public async Task ListVersions_ReturnsRecordedVersions()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"versions-{Guid.NewGuid():N}");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>();
        versions.Should().NotBeNull();
        versions!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetVersion_ReturnsSpecificVersion()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"version-get-{Guid.NewGuid():N}");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/1",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = await response.Content.ReadFromJsonAsync<ContentVersionDto>();
        version!.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task CompareVersions_ReturnsFieldChanges()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"compare-{Guid.NewGuid():N}");

        var updatedResponse = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{created.Id}",
            authorToken,
            new
            {
                slug = created.Slug,
                data = new Dictionary<string, object?> { ["title"] = "Changed", ["body"] = "Body" },
                changeSummary = "Title change",
                concurrencyToken = created.ConcurrencyToken,
            });
        var updated = await updatedResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, updated!);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/compare?left=1&right=2",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comparison = await response.Content.ReadFromJsonAsync<ContentVersionComparisonDto>();
        comparison!.Changes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RestoreVersion_RestoresHistoricalData()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"restore-ver-{Guid.NewGuid():N}");

        var updatedResponse = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{created.Id}",
            authorToken,
            new
            {
                slug = created.Slug,
                data = new Dictionary<string, object?> { ["title"] = "Changed", ["body"] = "Body" },
                changeSummary = "Title change",
                concurrencyToken = created.ConcurrencyToken,
            });
        var updated = await updatedResponse.Content.ReadFromJsonAsync<ContentEntryDto>();

        var versionResponse = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/1",
            editorToken);
        var versionOne = await versionResponse.Content.ReadFromJsonAsync<ContentVersionDto>();
        var expectedTitle = versionOne!.Data["title"]!.ToString();

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/1/restore",
            editorToken,
            new { changeSummary = "Restore version 1", concurrencyToken = updated!.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        entry!.DraftData["title"]!.ToString().Should().Be(expectedTitle);
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var (contentType, _, _, _) = await _scenario.SeedAsync();
        var viewerToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content",
            viewerToken,
            new
            {
                contentTypeId = contentType.Id,
                slug = $"forbidden-{Guid.NewGuid():N}",
                data = new Dictionary<string, object?> { ["title"] = "X", ["body"] = "Y" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Publish_AsAuthor_Returns403()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"auth-pub-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, created.Id, created.ConcurrencyToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/publish",
            authorToken,
            new { changeSummary = "Attempt", concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AsAuthor_WhenNotOwner_Returns403()
    {
        var (contentType, adminToken, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(adminToken, contentType.Id, $"owner-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{created.Id}", authorToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_NonExistentEntry_Returns404()
    {
        var (_, _, _, editorToken) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{Guid.NewGuid()}", editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_NonExistentEntry_Returns404()
    {
        var (_, _, _, editorToken) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{Guid.NewGuid()}",
            editorToken,
            new
            {
                slug = "missing",
                data = new Dictionary<string, object?> { ["title"] = "X" },
                changeSummary = "Update",
                concurrencyToken = 1u,
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentEntry_Returns404()
    {
        var (_, _, _, editorToken) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content/{Guid.NewGuid()}",
            editorToken,
            new { concurrencyToken = 1u });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_NonExistentEntry_Returns404()
    {
        var (_, _, _, editorToken) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{Guid.NewGuid()}/publish",
            editorToken,
            new { changeSummary = "Publish", concurrencyToken = 1u });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersion_NonExistent_Returns404()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"ver404-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/999",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithStaleConcurrencyToken_Returns409()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"conflict-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{created.Id}",
            authorToken,
            new
            {
                slug = created.Slug,
                data = new Dictionary<string, object?> { ["title"] = "Conflict", ["body"] = "Body" },
                changeSummary = "Conflict update",
                concurrencyToken = created.ConcurrencyToken + 10,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithStaleConcurrencyToken_Returns409()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"del-conflict-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content/{created.Id}",
            editorToken,
            new { concurrencyToken = created.ConcurrencyToken + 10 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_MissingRequiredField_Returns422()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content",
            authorToken,
            new
            {
                contentTypeId = contentType.Id,
                slug = $"invalid-{Guid.NewGuid():N}",
                data = new Dictionary<string, object?> { ["body"] = "No title" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns422()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var slug = $"dup-{Guid.NewGuid():N}";
        await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content",
            authorToken,
            new
            {
                contentTypeId = contentType.Id,
                slug,
                data = new Dictionary<string, object?> { ["title"] = "Another", ["body"] = "Body" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Publish_FromDraft_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"bad-pub-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/publish",
            editorToken,
            new { changeSummary = "Invalid transition", concurrencyToken = created.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = string.Join(' ', problem.GetProperty("errors").EnumerateObject().SelectMany(p => p.Value.EnumerateArray()).Select(e => e.GetString()));
        errors.Should().Contain("Invalid content status transition");
    }

    [Fact]
    public async Task Update_WithEmptyChangeSummary_Returns422()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"val-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{created.Id}",
            authorToken,
            new
            {
                slug = created.Slug,
                data = new Dictionary<string, object?> { ["title"] = "X", ["body"] = "Y" },
                changeSummary = string.Empty,
                concurrencyToken = created.ConcurrencyToken,
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task List_WithUnsupportedSort_Returns400()
    {
        var (_, _, _, editorToken) = await _scenario.SeedAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            "/api/v1/content?sortBy=unsupportedField",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
