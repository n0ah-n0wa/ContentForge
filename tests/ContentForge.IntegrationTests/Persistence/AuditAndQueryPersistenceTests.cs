namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentAssertions;

public sealed class AuditPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task AuditService_PersistsImmutableAuditEntries()
    {
        await using var scope = CreatePersistenceScope();

        await scope.AuditService.RecordAsync(
            AuditAction.ContentCreated,
            entityType: "ContentEntry",
            entityId: Guid.NewGuid().ToString(),
            userId: PersistenceTestConstants.ActorId,
            metadata: """{"slug":"audit-entry"}""",
            ipAddress: "127.0.0.1",
            userAgent: "integration-test",
            correlationId: "persistence-correlation");

        await scope.UnitOfWork.SaveChangesAsync();

        var logs = await scope.AuditLogs.ListAsync(new AuditLogListCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            SortRequest.Default("timestamp"),
            UserId: PersistenceTestConstants.ActorId,
            Action: AuditAction.ContentCreated,
            EntityType: "ContentEntry"));

        logs.Items.Should().HaveCount(1);
        logs.Items[0].Metadata.Should().Contain("audit-entry");
        logs.Items[0].IpAddress.Should().Be("127.0.0.1");
        logs.Items[0].CorrelationId.Should().Be("persistence-correlation");
    }

    [Fact]
    public async Task AuditLogs_CannotBeModifiedOrDeleted()
    {
        await using var scope = CreatePersistenceScope();

        await scope.AuditService.RecordAsync(
            AuditAction.ContentUpdated,
            entityType: "ContentEntry",
            entityId: "immutable-entry",
            userId: PersistenceTestConstants.ActorId);

        var entity = scope.DbContext.AuditLogs.Single();
        entity.Metadata = "tampered";

        var modify = () => scope.DbContext.SaveChangesAsync();
        await modify.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");

        scope.DbContext.ChangeTracker.Clear();
        var reloaded = scope.DbContext.AuditLogs.Single();
        scope.DbContext.AuditLogs.Remove(reloaded);

        var delete = () => scope.DbContext.SaveChangesAsync();
        await delete.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    [Fact]
    public async Task AuditLogs_SupportTimestampFiltering()
    {
        await using var scope = CreatePersistenceScope();

        scope.DbContext.AuditLogs.AddRange(
            new Infrastructure.Persistence.Entities.AuditLogEntity
            {
                Id = Guid.NewGuid(),
                Timestamp = PersistenceTestConstants.BaseTimestamp,
                Action = AuditAction.ContentUpdated.ToString(),
                EntityType = "ContentEntry",
                EntityId = "entry-1",
            },
            new Infrastructure.Persistence.Entities.AuditLogEntity
            {
                Id = Guid.NewGuid(),
                Timestamp = PersistenceTestConstants.BaseTimestamp.AddHours(2),
                Action = AuditAction.ContentUpdated.ToString(),
                EntityType = "ContentEntry",
                EntityId = "entry-2",
            });

        await scope.UnitOfWork.SaveChangesAsync();

        var filtered = await scope.AuditLogs.ListAsync(new AuditLogListCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            SortRequest.Default("timestamp"),
            From: PersistenceTestConstants.BaseTimestamp.AddHours(1),
            To: PersistenceTestConstants.BaseTimestamp.AddHours(3)));

        filtered.Items.Should().HaveCount(1);
        filtered.Items[0].EntityId.Should().Be("entry-2");
    }
}

public sealed class QueryPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntries_SupportPaginationAndSorting()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "alpha"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "beta"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "gamma"));
        await scope.UnitOfWork.SaveChangesAsync();

        var page = await scope.ContentEntries.ListAsync(new ContentEntryListCriteria(
            new PaginationRequest(Page: 1, PageSize: 2),
            new SortRequest("slug", SortDirection.Asc),
            ContentTypeId: contentType.Id,
            Status: ContentStatus.Draft));

        page.TotalItems.Should().Be(3);
        page.Items.Should().HaveCount(2);
        page.Items[0].Slug.Value.Should().Be("alpha");
        page.Items[1].Slug.Value.Should().Be("beta");
    }

    [Fact]
    public async Task ContentEntries_SupportSearchFiltering()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "alpha"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "beta"));
        await scope.UnitOfWork.SaveChangesAsync();

        var filtered = await scope.ContentEntries.ListAsync(new ContentEntryListCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            SortRequest.Default("slug"),
            ContentTypeId: contentType.Id,
            Search: "beta"));

        filtered.Items.Should().ContainSingle();
        filtered.Items[0].Slug.Value.Should().Be("beta");
    }

    [Fact]
    public async Task ContentTypes_SupportActiveFilterAndSearch()
    {
        await using var scope = CreatePersistenceScope();

        var active = PersistenceTestDataFactory.CreateArticleContentType(name: "newsItem", slug: "news-item");
        var inactive = PersistenceTestDataFactory.CreateArticleContentType(name: "archiveItem", slug: "archive-item");
        inactive.Deactivate(PersistenceTestConstants.ActorId, PersistenceTestConstants.BaseTimestamp);

        await scope.ContentTypes.AddAsync(active);
        await scope.ContentTypes.AddAsync(inactive);
        await scope.UnitOfWork.SaveChangesAsync();

        var results = await scope.ContentTypes.ListAsync(new ContentTypeListCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            new SortRequest("name", SortDirection.Asc),
            IsActive: true,
            Search: "news"));

        results.Items.Should().ContainSingle();
        results.Items[0].ContentType.Name.Should().Be(FieldName.Create("newsItem"));
    }
}
