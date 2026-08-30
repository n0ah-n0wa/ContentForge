namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Content;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class ContentVersionApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ContentVersionApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task CreateAndEdits_ProduceSequentialCompleteSnapshots()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"seq-{Guid.NewGuid():N}", "Title one");

        var firstEdit = await UpdateAsync(authorToken, created, "Title two", "Second save");
        await UpdateAsync(authorToken, firstEdit, "Title three", "Third save");

        var versions = await ListVersionsAsync(editorToken, created.Id);
        versions.Select(version => version.VersionNumber).Should().Equal(1, 2, 3);
        versions[0].Data["title"]!.ToString().Should().Be("Title one");
        versions[0].Slug.Should().Be(created.Slug);
        versions[0].Status.Should().Be(ContentStatus.Draft);
        versions[0].ChangeSummary.Should().Be("Created");
        versions[1].Data["title"]!.ToString().Should().Be("Title two");
        versions[2].Data["title"]!.ToString().Should().Be("Title three");
        versions.Should().OnlyContain(version => version.ContentEntryId == created.Id);
    }

    [Fact]
    public async Task HistoricalVersions_RemainUnchangedAfterLaterEdits()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"immut-{Guid.NewGuid():N}", "Original");
        var updated = await UpdateAsync(authorToken, created, "Changed", "Change title");

        var original = await GetVersionAsync(editorToken, created.Id, 1);
        original.Data["title"]!.ToString().Should().Be("Original");

        await UpdateAsync(authorToken, updated, "Changed again", "Another change");

        var originalAfter = await GetVersionAsync(editorToken, created.Id, 1);
        originalAfter.Id.Should().Be(original.Id);
        originalAfter.Data["title"]!.ToString().Should().Be("Original");
        originalAfter.ChangeSummary.Should().Be("Created");
    }

    [Fact]
    public async Task Restore_CreatesNewVersionAndLeavesHistoryIntact()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"restore-{Guid.NewGuid():N}", "Original");
        var updated = await UpdateAsync(authorToken, created, "Changed", "Change title");

        var restored = await RestoreAsync(editorToken, created.Id, 1, updated.ConcurrencyToken, "Roll back");
        restored.DraftData["title"]!.ToString().Should().Be("Original");
        restored.CurrentVersion.Should().Be(3);

        var versions = await ListVersionsAsync(editorToken, created.Id);
        versions.Should().HaveCount(3);
        versions[0].Data["title"]!.ToString().Should().Be("Original");
        versions[1].Data["title"]!.ToString().Should().Be("Changed");
        versions[2].Data["title"]!.ToString().Should().Be("Original");
        versions[2].ChangeSummary.Should().Contain("Restored from version 1");
    }

    [Fact]
    public async Task RestoreThenEdit_AppendsAnotherVersionWithoutRewritingHistory()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"restore-edit-{Guid.NewGuid():N}", "Original");
        var updated = await UpdateAsync(authorToken, created, "Changed", "Change title");
        var restored = await RestoreAsync(editorToken, created.Id, 1, updated.ConcurrencyToken, "Roll back");

        await UpdateAsync(authorToken, restored, "After restore", "Post-restore edit");

        var versions = await ListVersionsAsync(editorToken, created.Id);
        versions.Select(version => version.VersionNumber).Should().Equal(1, 2, 3, 4);
        versions[0].Data["title"]!.ToString().Should().Be("Original");
        versions[1].Data["title"]!.ToString().Should().Be("Changed");
        versions[3].Data["title"]!.ToString().Should().Be("After restore");
    }

    [Fact]
    public async Task Publish_CreatesPublishedVersionSnapshot()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"pub-ver-{Guid.NewGuid():N}", "Publish me");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);

        var versions = await ListVersionsAsync(editorToken, created.Id);
        versions.Select(version => version.VersionNumber).Should().Equal(1, 2, 3);
        versions[2].Status.Should().Be(ContentStatus.Published);
        versions[2].Data["title"]!.ToString().Should().Be("Publish me");
        versions[2].CreatedBy.Should().Be(AuthTestConstants.EditorUserId);
    }

    [Fact]
    public async Task CompareVersions_ReturnsFieldDifferences()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"cmp-{Guid.NewGuid():N}", "Left title");
        await UpdateAsync(authorToken, created, "Right title", "Change title");

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/compare?left=1&right=2",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var comparison = await response.Content.ReadFromJsonAsync<ContentVersionComparisonDto>();
        comparison!.LeftVersionNumber.Should().Be(1);
        comparison.RightVersionNumber.Should().Be(2);
        comparison.Changes.Should().Contain(change => change.FieldName == "title");
    }

    [Fact]
    public async Task Restore_UnknownVersion_Returns404()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"missing-ver-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/99/restore",
            editorToken,
            new { changeSummary = "Restore missing", concurrencyToken = created.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_VersionZero_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"zero-ver-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/0/restore",
            editorToken,
            new { changeSummary = "Restore zero", concurrencyToken = created.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Restore_AsAuthor_Returns403()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"auth-restore-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/1/restore",
            authorToken,
            new { changeSummary = "Author restore", concurrencyToken = created.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Restore_AsViewer_Returns403()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"view-restore-{Guid.NewGuid():N}");
        var viewerToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/1/restore",
            viewerToken,
            new { changeSummary = "Viewer restore", concurrencyToken = created.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListVersions_AsViewer_Returns200()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"view-list-{Guid.NewGuid():N}");
        var viewerToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.ViewerEmail,
            AuthTestConstants.ViewerPassword);

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{created.Id}/versions", viewerToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>();
        versions.Should().ContainSingle();
    }

    [Fact]
    public async Task ListVersions_AsAuthor_WhenNotOwner_Returns403()
    {
        var (contentType, adminToken, authorToken, _) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(adminToken, contentType.Id, $"owner-ver-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{created.Id}/versions", authorToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Compare_InvalidVersionNumber_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"cmp-bad-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{created.Id}/versions/compare?left=0&right=1",
            editorToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<ContentEntryDto> UpdateAsync(
        string token,
        ContentEntryDto entry,
        string title,
        string changeSummary)
    {
        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{entry.Id}",
            token,
            new
            {
                slug = entry.Slug,
                data = new Dictionary<string, object?> { ["title"] = title, ["body"] = "Draft body" },
                changeSummary,
                concurrencyToken = entry.ConcurrencyToken,
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> RestoreAsync(
        string token,
        Guid entryId,
        int versionNumber,
        uint concurrencyToken,
        string changeSummary)
    {
        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{entryId}/versions/{versionNumber}/restore",
            token,
            new { changeSummary, concurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<IReadOnlyList<ContentVersionDto>> ListVersionsAsync(string token, Guid entryId)
    {
        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{entryId}/versions", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>())!;
    }

    private async Task<ContentVersionDto> GetVersionAsync(string token, Guid entryId, int versionNumber)
    {
        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{entryId}/versions/{versionNumber}",
            token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentVersionDto>())!;
    }
}
