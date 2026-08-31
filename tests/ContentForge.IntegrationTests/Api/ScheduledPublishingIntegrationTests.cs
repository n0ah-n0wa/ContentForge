namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.Audit.Models;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Content;
using ContentForge.Domain.Scheduling;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;

[Collection(PersistenceTests.Name)]
public sealed class ScheduledPublishingIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentApiScenario _scenario = null!;

    public ScheduledPublishingIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
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
    public async Task ScheduledPublication_PublishesContentWhenJobBecomesDue()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"sched-pub-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug, "Scheduled headline");

        var publishAt = DateTimeOffset.UtcNow.AddHours(2);
        var (scheduleResponse, scheduled) = await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            publishAt,
            unpublishAt: null);

        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheduled!.ScheduledPublishAt.Should().BeCloseTo(publishAt, TimeSpan.FromSeconds(1));
        scheduled.ScheduledUnpublishAt.Should().BeNull();
        scheduled.Status.Should().Be(ContentStatus.Draft);

        var jobsBefore = await ScheduledPublishingTestHelper.GetJobsAsync(_factory.Services, draft.Id);
        jobsBefore.Should().ContainSingle(job =>
            job.JobType == ScheduledJobType.ContentPublish.ToString()
            && job.Status == ScheduledJobStatus.Pending.ToString());

        await ScheduledPublishingTestHelper.MakeJobsDueAsync(_factory.Services, draft.Id);
        var processed = await ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services);
        processed.Should().Be(1);

        var getResponse = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{draft.Id}", editorToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await getResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        published!.Status.Should().Be(ContentStatus.Published);
        published.ScheduledPublishAt.Should().BeNull();
        published.PublishedAt.Should().NotBeNull();

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);

        var jobsAfter = await ScheduledPublishingTestHelper.GetJobsAsync(_factory.Services, draft.Id);
        jobsAfter.Should().ContainSingle(job =>
            job.JobType == ScheduledJobType.ContentPublish.ToString()
            && job.Status == ScheduledJobStatus.Completed.ToString());

        var audit = await ListAuditAsync(adminToken, AuditAction.ContentPublished, draft.Id);
        audit.Should().ContainSingle();
    }

    [Fact]
    public async Task ScheduledUnpublication_UnpublishesContentWhenJobBecomesDue()
    {
        var (contentType, adminToken, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"sched-unpub-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        var published = await _scenario.AdvanceToPublishedAsync(authorToken, editorToken, draft);

        var unpublishAt = DateTimeOffset.UtcNow.AddHours(3);
        var (scheduleResponse, scheduled) = await _scenario.SchedulePublishingAsync(
            editorToken,
            published.Id,
            published.ConcurrencyToken,
            publishAt: null,
            unpublishAt);

        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheduled!.ScheduledUnpublishAt.Should().BeCloseTo(unpublishAt, TimeSpan.FromSeconds(1));
        scheduled.Status.Should().Be(ContentStatus.Published);

        await ScheduledPublishingTestHelper.MakeJobsDueAsync(_factory.Services, published.Id);
        var processed = await ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services);
        processed.Should().Be(1);

        var getResponse = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content/{published.Id}", editorToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await getResponse.Content.ReadFromJsonAsync<ContentEntryDto>();
        result!.Status.Should().Be(ContentStatus.Draft);
        result.ScheduledUnpublishAt.Should().BeNull();
        result.PublishedData.Should().BeNull();

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var audit = await ListAuditAsync(adminToken, AuditAction.ContentUnpublished, published.Id);
        audit.Should().ContainSingle();
    }

    [Fact]
    public async Task DuplicateExecution_DoesNotProcessCompletedJobTwice()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"dup-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        var publishAt = DateTimeOffset.UtcNow.AddHours(1);

        await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            publishAt,
            unpublishAt: null);

        await ScheduledPublishingTestHelper.MakeJobsDueAsync(_factory.Services, draft.Id);

        var firstPass = await ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services);
        var secondPass = await ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services);

        firstPass.Should().Be(1);
        secondPass.Should().Be(0);

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DuplicateExecution_PreventsParallelClaimsOnSameJob()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var slug = $"parallel-{Guid.NewGuid():N}";
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
        var publishAt = DateTimeOffset.UtcNow.AddHours(1);

        await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            publishAt,
            unpublishAt: null);

        await ScheduledPublishingTestHelper.MakeJobsDueAsync(_factory.Services, draft.Id);

        var results = await Task.WhenAll(
            Task.Run(() => ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services)),
            Task.Run(() => ScheduledPublishingTestHelper.ProcessDueJobsAsync(_factory.Services)));

        results.Sum().Should().Be(1);

        var jobs = await ScheduledPublishingTestHelper.GetJobsAsync(_factory.Services, draft.Id);
        jobs.Count(job => job.Status == ScheduledJobStatus.Completed.ToString()).Should().Be(1);
        jobs.Count(job => job.Status == ScheduledJobStatus.Running.ToString()).Should().Be(0);

        var publicItem = await _client.GetAsync($"/api/v1/public/{contentType.Slug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RestartScenario_PendingJobSurvivesAndProcessesAfterNewHostStarts()
    {
        await _databaseFixture.ResetDatabaseAsync();

        var contentTypeSlug = string.Empty;
        var slug = string.Empty;
        var entryId = Guid.Empty;

        await using (var firstFactory = new ContentForgeWebApplicationFactory())
        {
            using var firstClient = firstFactory.CreateClient();
            await AuthTestSeeder.SeedAsync(firstFactory.Services);
            var scenario = new ContentApiScenario(firstClient);

            var (contentType, _, authorToken, editorToken) = await scenario.SeedAsync();
            contentTypeSlug = contentType.Slug;
            slug = $"restart-{Guid.NewGuid():N}";
            var draft = await scenario.CreateDraftAsync(authorToken, contentType.Id, slug);
            entryId = draft.Id;

            await scenario.SchedulePublishingAsync(
                editorToken,
                draft.Id,
                draft.ConcurrencyToken,
                DateTimeOffset.UtcNow.AddHours(4),
                unpublishAt: null);

            await ScheduledPublishingTestHelper.MakeJobsDueAsync(firstFactory.Services, entryId);

            var jobsBeforeRestart = await ScheduledPublishingTestHelper.GetJobsAsync(firstFactory.Services, entryId);
            jobsBeforeRestart.Should().ContainSingle(job => job.Status == ScheduledJobStatus.Pending.ToString());
        }

        await using var secondFactory = new ContentForgeWebApplicationFactory();
        using var secondClient = secondFactory.CreateClient();

        var jobsAfterRestart = await ScheduledPublishingTestHelper.GetJobsAsync(secondFactory.Services, entryId);
        jobsAfterRestart.Should().ContainSingle(job => job.Status == ScheduledJobStatus.Pending.ToString());

        var processed = await ScheduledPublishingTestHelper.ProcessDueJobsAsync(secondFactory.Services);
        processed.Should().Be(1);

        var publicItem = await secondClient.GetAsync($"/api/v1/public/{contentTypeSlug}/{slug}");
        publicItem.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvalidSchedule_PublishAtInPast_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"past-{Guid.NewGuid():N}");

        var (response, _) = await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            unpublishAt: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task InvalidSchedule_UnpublishBeforePublish_Returns422()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"order-{Guid.NewGuid():N}");

        var publishAt = DateTimeOffset.UtcNow.AddHours(2);
        var unpublishAt = DateTimeOffset.UtcNow.AddHours(1);

        var (response, _) = await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            publishAt,
            unpublishAt);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ClearSchedule_CancelsPendingJobs()
    {
        var (contentType, _, authorToken, editorToken) = await _scenario.SeedAsync();
        var draft = await _scenario.CreateDraftAsync(authorToken, contentType.Id, $"clear-{Guid.NewGuid():N}");

        var publishAt = DateTimeOffset.UtcNow.AddHours(6);
        var (scheduleResponse, scheduled) = await _scenario.SchedulePublishingAsync(
            editorToken,
            draft.Id,
            draft.ConcurrencyToken,
            publishAt,
            unpublishAt: null);

        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheduled!.ScheduledPublishAt.Should().NotBeNull();

        var cleared = await _scenario.ClearScheduleAsync(editorToken, draft.Id, scheduled.ConcurrencyToken);
        cleared.ScheduledPublishAt.Should().BeNull();

        var jobs = await ScheduledPublishingTestHelper.GetJobsAsync(_factory.Services, draft.Id);
        jobs.Should().ContainSingle(job => job.Status == ScheduledJobStatus.Cancelled.ToString());
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
