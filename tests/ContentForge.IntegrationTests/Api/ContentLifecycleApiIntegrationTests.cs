namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Audit.Models;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ContentLifecycleApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ContentLifecycleApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task Publish_ExecutesFullLifecycleContract()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"pub-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Live title");

        var publicBefore = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicBefore.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var submitted = await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);
        submitted.Status.Should().Be(ContentStatus.InReview);

        var published = await _scenario.PublishAsync(editorToken, submitted.Id, submitted.ConcurrencyToken, "Initial publication");
        published.Status.Should().Be(ContentStatus.Published);
        published.PublishedAt.Should().NotBeNull();
        published.PublishedBy.Should().Be(AuthTestConstants.EditorUserId);
        published.PublishedData.Should().NotBeNull();
        published.PublishedData!["title"]!.ToString().Should().Be("Live title");
        published.CurrentVersion.Should().BeGreaterThan(0);

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicDto = await publicItem.Content.ReadFromJsonAsync<PublicContentDto>();
        publicDto!.Slug.Should().Be(slug);
        publicDto.Data["title"]!.ToString().Should().Be("Live title");
        publicDto.PublishedAt.Should().BeCloseTo(published.PublishedAt!.Value, TimeSpan.FromSeconds(1));

        var versionsResponse = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{published.Id}/versions",
            editorToken);
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>();
        versions.Should().Contain(version => version.Status == ContentStatus.Published);
        var publishedVersion = versions!.Single(version => version.Status == ContentStatus.Published);
        publishedVersion.ChangeSummary.Should().Be("Initial publication");
        publishedVersion.CreatedBy.Should().Be(AuthTestConstants.EditorUserId);

        var audit = await ListAuditAsync(adminToken, AuditAction.ContentPublished, published.Id);
        audit.Should().ContainSingle(entry =>
            entry.Action == AuditAction.ContentPublished
            && entry.EntityId == published.Id.ToString()
            && entry.UserId == AuthTestConstants.EditorUserId);
    }

    [Fact]
    public async Task DraftEditAfterPublish_DoesNotChangePublicRepresentation()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"edit-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Published title");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var updateResponse = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{published.Id}",
            authorToken,
            new
            {
                slug,
                data = new Dictionary<string, object?> { ["title"] = "Draft-only title", ["body"] = "Draft body" },
                changeSummary = "Working copy",
                concurrencyToken = published.ConcurrencyToken,
            });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var workingCopy = await updateResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        workingCopy!.Status.Should().Be(ContentStatus.Draft);
        workingCopy.DraftData["title"]!.ToString().Should().Be("Draft-only title");
        workingCopy.PublishedData!["title"]!.ToString().Should().Be("Published title");

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicDto = await publicItem.Content.ReadFromJsonAsync<PublicContentDto>();
        publicDto!.Data["title"]!.ToString().Should().Be("Published title");
    }

    [Fact]
    public async Task Unpublish_RemovesPublicContentPreservesVersionsAndAudits()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"unpub-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var versionsBefore = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{published.Id}/versions",
            editorToken);
        var before = await versionsBefore.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>();

        var unpublished = await _scenario.UnpublishAsync(editorToken, published.Id, published.ConcurrencyToken);
        unpublished.Status.Should().Be(ContentStatus.Draft);
        unpublished.PublishedData.Should().BeNull();
        unpublished.PublishedAt.Should().BeNull();
        unpublished.PublishedBy.Should().BeNull();

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var versionsAfter = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{unpublished.Id}/versions",
            editorToken);
        var after = await versionsAfter.Content.ReadFromJsonAsync<IReadOnlyList<ContentVersionDto>>();
        after!.Count.Should().BeGreaterThan(before!.Count);
        after.Should().Contain(version => version.Status == ContentStatus.Published);

        var firstPublished = after.First(version => version.Status == ContentStatus.Published);
        var getHistorical = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content/{unpublished.Id}/versions/{firstPublished.VersionNumber}",
            editorToken);
        getHistorical.StatusCode.Should().Be(HttpStatusCode.OK);

        var audit = await ListAuditAsync(adminToken, AuditAction.ContentUnpublished, unpublished.Id);
        audit.Should().ContainSingle(entry => entry.Action == AuditAction.ContentUnpublished);
    }

    [Fact]
    public async Task Unpublish_RequiresReviewBeforeRepublish()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"repub-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);
        var unpublished = await _scenario.UnpublishAsync(editorToken, published.Id, published.ConcurrencyToken);

        var directPublish = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{unpublished.Id}/publish",
            editorToken,
            new { changeSummary = "Skip review", concurrencyToken = unpublished.ConcurrencyToken });
        directPublish.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var submitted = await _scenario.SubmitForReviewAsync(authorToken, unpublished.Id, unpublished.ConcurrencyToken);
        var republished = await _scenario.PublishAsync(editorToken, submitted.Id, submitted.ConcurrencyToken);
        republished.Status.Should().Be(ContentStatus.Published);
        republished.PublishedData.Should().NotBeNull();
        republished.PublishedAt.Should().NotBeNull();
        republished.PublishedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task Archive_RemovesPublicContentAndRestoreReturnsDraft()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"arch-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var archived = await _scenario.ArchiveAsync(editorToken, published.Id, published.ConcurrencyToken);
        archived.Status.Should().Be(ContentStatus.Archived);
        archived.PublishedData.Should().BeNull();
        archived.PublishedAt.Should().BeNull();

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var restoreResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{archived.Id}/restore",
            editorToken,
            new { changeSummary = "Restore archived", concurrencyToken = archived.ConcurrencyToken });
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var restored = await restoreResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        restored!.Status.Should().Be(ContentStatus.Draft);
        restored.PublishedData.Should().BeNull();

        var publicAfterRestore = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicAfterRestore.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var audit = await ListAuditAsync(adminToken, AuditAction.ContentArchived, archived.Id);
        audit.Should().ContainSingle();
    }

    [Fact]
    public async Task WithdrawFromReview_ReturnsDraft()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"withdraw-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/withdraw-from-review",
            authorToken,
            new { concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var withdrawn = await response.Content.ReadFromJsonAsync<ContentEntryDto>();
        withdrawn!.Status.Should().Be(ContentStatus.Draft);
    }

    [Theory]
    [InlineData("publish", "Invalid publish")]
    [InlineData("archive", "Invalid archive")]
    [InlineData("unpublish", "Invalid unpublish")]
    [InlineData("restore", "Invalid restore")]
    public async Task InvalidTransitions_FromDraft_Return422(string action, string changeSummary)
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"bad-{action}-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/{action}",
            editorToken,
            new { changeSummary, concurrencyToken = draft.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status422UnprocessableEntity);
        var errors = string.Join(
            ' ',
            problem.GetProperty("errors").EnumerateObject().SelectMany(property => property.Value.EnumerateArray()).Select(value => value.GetString()));
        errors.Should().Contain("Invalid content status transition");
    }

    [Fact]
    public async Task Archive_FromInReview_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"rev-arch-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/archive",
            editorToken,
            new { changeSummary = "Archive review", concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Publish_WhenSchemaRequiresNewField_Returns422()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"schema-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);

        await _scenario.AddFieldAsync(adminToken, contentType.Id, "subtitle", FieldType.Text, required: true);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/publish",
            editorToken,
            new { changeSummary = "Publish stale draft", concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SubmitForReview_WhenSchemaRequiresNewField_Returns422()
    {
        var (contentType, adminToken, authorToken, _) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"submit-val-{Guid.NewGuid():N}");

        await _scenario.AddFieldAsync(adminToken, contentType.Id, "kicker", FieldType.Text, required: true);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{draft.Id}/submit-for-review",
            authorToken,
            new { concurrencyToken = draft.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Publish_WhenContentTypeDeactivated_Returns422()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"inactive-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, draft.Id, draft.ConcurrencyToken);

        var deactivate = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentType.Id}/deactivate",
            adminToken);
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/publish",
            editorToken,
            new { changeSummary = "Publish inactive type", concurrencyToken = submitted.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Unpublish_EmptyChangeSummary_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"summary-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/unpublish",
            editorToken,
            new { changeSummary = string.Empty, concurrencyToken = published.ConcurrencyToken });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<IReadOnlyList<AuditLogEntryDto>> ListAuditAsync(
        string adminToken,
        AuditAction action,
        Guid entityId)
    {
        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/audit?action={action}&entityType=ContentEntry&entityId={entityId}&page=1&pageSize=50",
            adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLogEntryDto>>();
        return page!.Items;
    }
}
