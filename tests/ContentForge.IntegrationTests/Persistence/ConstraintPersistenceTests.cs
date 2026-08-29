namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ConstraintPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentType_EnforcesUniqueName()
    {
        await using var seedScope = CreatePersistenceScope();
        await seedScope.ContentTypes.AddAsync(PersistenceTestDataFactory.CreateArticleContentType(name: "article", slug: "article"));
        await seedScope.UnitOfWork.SaveChangesAsync();

        await using var duplicateScope = CreatePersistenceScope();
        await duplicateScope.ContentTypes.AddAsync(
            PersistenceTestDataFactory.CreateArticleContentType(name: "article", slug: "article-copy"));

        var action = async () => await duplicateScope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ContentType_EnforcesUniqueSlug()
    {
        await using var seedScope = CreatePersistenceScope();
        await seedScope.ContentTypes.AddAsync(PersistenceTestDataFactory.CreateArticleContentType(name: "articleOne", slug: "article"));
        await seedScope.UnitOfWork.SaveChangesAsync();

        await using var duplicateScope = CreatePersistenceScope();
        await duplicateScope.ContentTypes.AddAsync(
            PersistenceTestDataFactory.CreateArticleContentType(name: "articleTwo", slug: "article"));

        var action = async () => await duplicateScope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ContentEntry_EnforcesUniqueSlugPerContentType()
    {
        await using var seedScope = CreatePersistenceScope();
        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await seedScope.ContentTypes.AddAsync(contentType);
        await seedScope.UnitOfWork.SaveChangesAsync();

        await seedScope.ContentEntries.AddAsync(
            PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "duplicate-slug"));
        await seedScope.UnitOfWork.SaveChangesAsync();

        await using var duplicateScope = CreatePersistenceScope();
        await duplicateScope.ContentEntries.AddAsync(
            PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "duplicate-slug"));

        var action = async () => await duplicateScope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ContentEntry_EnforcesContentTypeForeignKey()
    {
        await using var scope = CreatePersistenceScope();

        var entry = ContentEntry.Create(
            ContentTypeId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            Slug.Create("orphan-entry"),
            PersistenceTestConstants.ActorId);

        await scope.ContentEntries.AddAsync(entry);

        var action = async () => await scope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ContentEntryRelations_EnforceUniqueSourceTargetFieldCombination()
    {
        await using var seedScope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await seedScope.ContentTypes.AddAsync(contentType);
        await seedScope.UnitOfWork.SaveChangesAsync();

        var source = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "source");
        var target = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "target");
        await seedScope.ContentEntries.AddAsync(source);
        await seedScope.ContentEntries.AddAsync(target);
        await seedScope.UnitOfWork.SaveChangesAsync();

        seedScope.DbContext.ContentEntryRelations.Add(
            PersistenceTestDataFactory.CreateRelation(source.Id.Value, target.Id.Value));
        await seedScope.UnitOfWork.SaveChangesAsync();

        await using var duplicateScope = CreatePersistenceScope();
        duplicateScope.DbContext.ContentEntryRelations.Add(
            PersistenceTestDataFactory.CreateRelation(source.Id.Value, target.Id.Value));

        var action = async () => await duplicateScope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }
}
