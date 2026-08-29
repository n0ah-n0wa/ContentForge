namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentAssertions;

public sealed class ContentEntryPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntry_PersistsDynamicJsonDataAndReloads()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "hello-world");
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        loaded.Should().NotBeNull();
        loaded!.Slug.Should().Be(Slug.Create("hello-world"));
        loaded.Status.Should().Be(ContentStatus.Draft);
        loaded.DraftData.GetValue("title").Should().Be("Hello World");
        loaded.DraftData.GetValue("body").Should().Be("Draft body");
        loaded.ConcurrencyToken.Should().Be(ConcurrencyToken.Initial);
    }

    [Fact]
    public async Task ContentEntry_GetBySlugRespectsContentTypeScope()
    {
        await using var scope = CreatePersistenceScope();

        var articleType = PersistenceTestDataFactory.CreateArticleContentType(name: "articleA", slug: "article-a");
        var pageType = PersistenceTestDataFactory.CreateArticleContentType(name: "pageA", slug: "page-a");
        await scope.ContentTypes.AddAsync(articleType);
        await scope.ContentTypes.AddAsync(pageType);
        await scope.UnitOfWork.SaveChangesAsync();

        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(articleType.Id, slug: "shared-slug"));
        await scope.ContentEntries.AddAsync(PersistenceTestDataFactory.CreateDraftEntry(pageType.Id, slug: "shared-slug"));
        await scope.UnitOfWork.SaveChangesAsync();

        var articleEntry = await scope.ContentEntries.GetBySlugAsync(articleType.Id, Slug.Create("shared-slug"));
        var pageEntry = await scope.ContentEntries.GetBySlugAsync(pageType.Id, Slug.Create("shared-slug"));

        articleEntry.Should().NotBeNull();
        pageEntry.Should().NotBeNull();
        articleEntry!.ContentTypeId.Should().Be(articleType.Id);
        pageEntry!.ContentTypeId.Should().Be(pageType.Id);
    }
}
