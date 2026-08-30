namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContentForge.Application.Content.Models;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ContentConcurrencyApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ContentConcurrencyApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task TwoClients_StaleUpdate_ReturnsStructured409AndKeepsWinner()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-{Guid.NewGuid():N}", "Original");
        var staleToken = created.ConcurrencyToken;

        var winnerResponse = await UpdateAsync(authorToken, created, staleToken, "Client A title");
        winnerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var winner = await winnerResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        winner!.DraftData["title"]!.ToString().Should().Be("Client A title");
        winner.ConcurrencyToken.Should().BeGreaterThan(staleToken);

        var staleResponse = await UpdateAsync(authorToken, created, staleToken, "Client B title");
        await AssertStructuredConflictAsync(staleResponse, staleToken, winner.ConcurrencyToken);

        var current = await GetAsync(editorToken, created.Id);
        current.DraftData["title"]!.ToString().Should().Be("Client A title");
        current.ConcurrencyToken.Should().Be(winner.ConcurrencyToken);
    }

    [Fact]
    public async Task TwoClients_StalePublish_ReturnsStructured409()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-pub-{Guid.NewGuid():N}");
        var submitted = await _scenario.SubmitForReviewAsync(authorToken, created.Id, created.ConcurrencyToken);
        var staleToken = submitted.ConcurrencyToken;

        var published = await _scenario.PublishAsync(editorToken, submitted.Id, staleToken, "Publisher A");

        var stalePublish = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{submitted.Id}/publish",
            editorToken,
            new { changeSummary = "Publisher B", concurrencyToken = staleToken });

        await AssertStructuredConflictAsync(stalePublish, staleToken, published.ConcurrencyToken);

        var current = await GetAsync(editorToken, created.Id);
        current.Status.Should().Be(Domain.Content.ContentStatus.Published);
        current.ConcurrencyToken.Should().Be(published.ConcurrencyToken);
    }

    [Fact]
    public async Task TwoClients_StaleArchive_ReturnsStructured409()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-arch-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);
        var staleToken = published.ConcurrencyToken;

        var archived = await _scenario.ArchiveAsync(editorToken, published.Id, staleToken);

        var staleArchive = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{published.Id}/archive",
            editorToken,
            new { changeSummary = "Second archive", concurrencyToken = staleToken });

        await AssertStructuredConflictAsync(staleArchive, staleToken, archived.ConcurrencyToken);
    }

    [Fact]
    public async Task TwoClients_StaleVersionRestore_ReturnsStructured409()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-restore-{Guid.NewGuid():N}", "Original");
        var staleToken = created.ConcurrencyToken;

        var editedResponse = await UpdateAsync(authorToken, created, staleToken, "Edited");
        var edited = await editedResponse.Content.ReadFromJsonAsync<ContentEntryDto>();

        var staleRestore = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{created.Id}/versions/1/restore",
            editorToken,
            new { changeSummary = "Stale restore", concurrencyToken = staleToken });

        await AssertStructuredConflictAsync(staleRestore, staleToken, edited!.ConcurrencyToken);

        var current = await GetAsync(editorToken, created.Id);
        current.DraftData["title"]!.ToString().Should().Be("Edited");
    }

    [Fact]
    public async Task TwoClients_StaleRestoreFromArchive_ReturnsStructured409()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-unarch-{Guid.NewGuid():N}");
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, created);
        var archived = await _scenario.ArchiveAsync(editorToken, published.Id, published.ConcurrencyToken);
        var staleToken = archived.ConcurrencyToken;

        var restoredResponse = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{archived.Id}/restore",
            editorToken,
            new { changeSummary = "Restored by A", concurrencyToken = staleToken });
        restoredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var restored = await restoredResponse.Content.ReadFromJsonAsync<ContentEntryDto>();

        var staleRestore = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content/{archived.Id}/restore",
            editorToken,
            new { changeSummary = "Restored by B", concurrencyToken = staleToken });

        await AssertStructuredConflictAsync(staleRestore, staleToken, restored!.ConcurrencyToken);

        var current = await GetAsync(editorToken, created.Id);
        current.Status.Should().Be(Domain.Content.ContentStatus.Draft);
        current.ConcurrencyToken.Should().Be(restored.ConcurrencyToken);
    }

    [Fact]
    public async Task ParallelUpdates_OnlyOneCommitWins()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var created = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"occ-race-{Guid.NewGuid():N}", "Original");
        var staleToken = created.ConcurrencyToken;

        var first = UpdateAsync(authorToken, created, staleToken, "Parallel A");
        var second = UpdateAsync(authorToken, created, staleToken, "Parallel B");
        var results = await Task.WhenAll(first, second);

        var statuses = results.Select(response => response.StatusCode).OrderBy(status => status).ToArray();
        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.Conflict);

        var conflict = results.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertStructuredConflictAsync(conflict, staleToken, expectedActual: null);

        var current = await GetAsync(editorToken, created.Id);
        current.DraftData["title"]!.ToString().Should().BeOneOf("Parallel A", "Parallel B");
        current.ConcurrencyToken.Should().BeGreaterThan(staleToken);
    }

    private async Task<HttpResponseMessage> UpdateAsync(
        string token,
        ContentEntryDto entry,
        uint concurrencyToken,
        string title) =>
        await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content/{entry.Id}",
            token,
            new
            {
                slug = entry.Slug,
                data = new Dictionary<string, object?> { ["title"] = title, ["body"] = "Draft body" },
                changeSummary = title,
                concurrencyToken,
            });

    private async Task<ContentEntryDto> GetAsync(string token, Guid id)
    {
        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{id}", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private static async Task AssertStructuredConflictAsync(
        HttpResponseMessage response,
        uint expectedVersion,
        uint? expectedActual)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().Should().Be("Conflict");
        problem.GetProperty("detail").GetString().Should().Contain("modified by another user");
        problem.GetProperty("expectedVersion").GetUInt32().Should().Be(expectedVersion);
        var actual = problem.GetProperty("actualVersion").GetUInt32();
        actual.Should().BeGreaterThan(expectedVersion);
        if (expectedActual is not null)
        {
            actual.Should().Be(expectedActual.Value);
        }

        problem.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }
}
