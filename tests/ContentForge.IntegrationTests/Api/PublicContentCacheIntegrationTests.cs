namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Abstractions.Caching;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Content;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

[Collection(PersistenceTests.Name)]
public sealed class PublicContentCacheIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;
    private IPublicContentCacheStatistics _cacheStatistics = null!;

    public PublicContentCacheIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
        _cacheStatistics = _factory.Services.GetRequiredService<IPublicContentCacheStatistics>();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetBySlug_SecondRequest_IsCacheHit()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"cache-hit-{Guid.NewGuid():N}"[..20];
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Cached title");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        _cacheStatistics.Reset();
        var url = $"/api/v1/public/{contentType.Slug}/{slug}";

        var first = await _client.GetAsync(url);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.GetAsync(url);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        _cacheStatistics.EntryMisses.Should().Be(1);
        _cacheStatistics.EntryHits.Should().Be(1);
    }

    [Fact]
    public async Task GetBySlug_FirstRequest_IsCacheMiss()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"cache-miss-{Guid.NewGuid():N}"[..20];
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Miss title");
        await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        _cacheStatistics.Reset();

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _cacheStatistics.EntryMisses.Should().Be(1);
        _cacheStatistics.EntryHits.Should().Be(0);
    }

    [Fact]
    public async Task GetBySlug_AfterRepublish_ReturnsUpdatedSnapshot()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"cache-repub-{Guid.NewGuid():N}"[..20];
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Original title");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var first = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        first.EnsureSuccessStatusCode();
        (await first.Content.ReadFromJsonAsync<PublicContentDto>())!.Data["title"]!.ToString().Should().Be("Original title");

        var updateResponse = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{published.Id}",
            authorToken,
            new
            {
                slug,
                data = new Dictionary<string, object?>
                {
                    ["title"] = "Updated title",
                    ["body"] = "Updated body",
                },
                changeSummary = "Updated draft",
                concurrencyToken = published.ConcurrencyToken,
            });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        updated!.Status.Should().Be(ContentStatus.Draft);

        var submitted = await _scenario.SubmitForReviewAsync(authorToken, updated.Id, updated.ConcurrencyToken);
        var republished = await _scenario.PublishAsync(editorToken, submitted.Id, submitted.ConcurrencyToken, "Republish");

        _cacheStatistics.Reset();
        var afterRepublish = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        afterRepublish.EnsureSuccessStatusCode();

        _cacheStatistics.EntryMisses.Should().Be(1);
        (await afterRepublish.Content.ReadFromJsonAsync<PublicContentDto>())!.Data["title"]!.ToString().Should().Be("Updated title");
        republished.ConcurrencyToken.Should().BeGreaterThan(published.ConcurrencyToken);
    }

    [Fact]
    public async Task GetBySlug_AfterUnpublish_Returns404AndDoesNotServeCachedPayload()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"cache-unpub-{Guid.NewGuid():N}"[..20];
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Will unpublish");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var cached = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        cached.EnsureSuccessStatusCode();

        var second = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        second.EnsureSuccessStatusCode();
        _cacheStatistics.EntryHits.Should().BeGreaterThan(0);

        var unpublishResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/unpublish",
            editorToken,
            new { changeSummary = "Unpublish", concurrencyToken = published.ConcurrencyToken });
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _cacheStatistics.Reset();
        var afterUnpublish = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        afterUnpublish.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _cacheStatistics.EntryMisses.Should().Be(1);
        _cacheStatistics.EntryHits.Should().Be(0);
    }

    [Fact]
    public async Task GetBySlug_DraftNeverCached_Returns404WithoutHittingCache()
    {
        var (contentType, _, authorToken, _) = await _scenario.SeedAsync();
        var slug = $"cache-draft-{Guid.NewGuid():N}"[..20];
        await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Draft only");

        _cacheStatistics.Reset();

        var response = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _cacheStatistics.EntryHits.Should().Be(0);
        _cacheStatistics.EntryMisses.Should().Be(1);
    }
}
