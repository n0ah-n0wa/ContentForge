namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Domain.Content;
using ContentForge.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class QueryPerformancePersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntries_ClampOversizedPageSize()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        for (var index = 0; index < 5; index++)
        {
            await scope.ContentEntries.AddAsync(
                PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: $"page-{index}"));
        }

        await scope.UnitOfWork.SaveChangesAsync();

        var page = await scope.ContentEntries.ListAsync(new ContentEntryListCriteria(
            new PaginationRequest(Page: 1, PageSize: 500),
            SortRequest.Default("slug"),
            ContentTypeId: contentType.Id));

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(PaginationDefaults.MaxPageSize);
        page.Items.Should().HaveCount(5);
        page.TotalItems.Should().Be(5);
    }

    [Fact]
    public async Task ContentEntries_NormalizeInvalidPageNumber()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "first"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "second"));
        await scope.UnitOfWork.SaveChangesAsync();

        var page = await scope.ContentEntries.ListAsync(new ContentEntryListCriteria(
            new PaginationRequest(Page: 0, PageSize: 1),
            new SortRequest("slug", SortDirection.Asc),
            ContentTypeId: contentType.Id));

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].Slug.Value.Should().Be("first");
    }

    [Fact]
    public async Task ContentSearch_TreatsLikeWildcardsLiterallyInDraftData()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(
            contentType.Id,
            slug: "literal-percent-match",
            data: ContentData.FromDictionary(new Dictionary<string, object?>
            {
                ["title"] = "Discount is 100% today",
                ["body"] = "No wildcard match",
            })));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(
            contentType.Id,
            slug: "literal-percent-other",
            data: ContentData.FromDictionary(new Dictionary<string, object?>
            {
                ["title"] = "Discount is 100x today",
                ["body"] = "Different title",
            })));
        await scope.UnitOfWork.SaveChangesAsync();

        var filtered = await scope.ContentSearch.SearchAsync(new ContentSearchCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            SortRequest.Default("slug"),
            ContentTypeId: contentType.Id,
            Keyword: "100%"));

        filtered.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ContentSearch_GeneratesParameterizedSqlWithFixedOrderBy()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var keyword = $"sql-safe-{Guid.NewGuid():N}";
        var pattern = PortableSearch.CreateContainsPattern(keyword);

        var query = scope.DbContext.ContentEntries
            .AsNoTracking()
            .Where(entry => !entry.IsDeleted)
            .Where(entry => entry.ContentTypeId == contentType.Id.Value)
            .Where(entry =>
                EF.Functions.ILike(entry.Slug, pattern, PortableSearch.LikeEscapeCharacter)
                || EF.Functions.ILike(entry.DraftDataJson, pattern, PortableSearch.LikeEscapeCharacter))
            .OrderBy(entry => entry.Slug)
            .Select(entry => entry.Id);

        var sql = query.ToQueryString();
        sql.Should().Contain("@__pattern");
        sql.Should().Contain("ORDER BY");
        sql.Should().NotContain("ORDER BY CASE");
        sql.Should().NotContain($"ILIKE '{keyword}'");
    }

    [Fact]
    public async Task ContentSearchService_ClampOversizedPageSize()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "searchable"));
        await scope.UnitOfWork.SaveChangesAsync();

        var result = await scope.ContentSearch.SearchAsync(new ContentSearchCriteria(
            new PaginationRequest(Page: 1, PageSize: 500),
            SortRequest.Default("slug"),
            ContentTypeId: contentType.Id,
            Keyword: "searchable"));

        result.PageSize.Should().Be(PaginationDefaults.MaxPageSize);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSummariesByIdsAsync_PreservesOrderWithoutLoadingVersions()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "alpha"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "beta"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "gamma"));
        await scope.UnitOfWork.SaveChangesAsync();

        var listed = await scope.ContentEntries.ListAsync(new ContentEntryListCriteria(
            new PaginationRequest(Page: 1, PageSize: 10),
            new SortRequest("slug", SortDirection.Asc),
            ContentTypeId: contentType.Id));

        var orderedIds = listed.Items.Select(entry => entry.Id).Reverse().ToList();
        var summaries = await scope.ContentEntries.GetSummariesByIdsAsync(orderedIds);

        summaries.Select(entry => entry.Id).Should().Equal(orderedIds);
        summaries.Should().OnlyContain(entry => entry.Versions.Count == 0);

        var idValues = orderedIds.Select(id => id.Value).ToList();
        var sql = scope.DbContext.ContentEntries
            .AsNoTracking()
            .Where(entry => idValues.Contains(entry.Id))
            .ToQueryString();

        sql.Should().Contain("ANY");
        sql.Should().NotContain("ContentVersions");
    }
}
