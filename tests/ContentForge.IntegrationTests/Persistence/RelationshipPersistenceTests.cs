namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class RelationshipPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntryRelations_EnforceForeignKeys()
    {
        await using var scope = CreatePersistenceScope();

        scope.DbContext.ContentEntryRelations.Add(
            PersistenceTestDataFactory.CreateRelation(Guid.NewGuid(), Guid.NewGuid()));

        var action = async () => await scope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ContentEntryRelations_PersistSourceAndTargetLinks()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var source = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "source-entry");
        var target = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "target-entry");
        await scope.ContentEntries.AddAsync(source);
        await scope.ContentEntries.AddAsync(target);
        await scope.UnitOfWork.SaveChangesAsync();

        scope.DbContext.ContentEntryRelations.Add(
            PersistenceTestDataFactory.CreateRelation(source.Id.Value, target.Id.Value));
        await scope.UnitOfWork.SaveChangesAsync();

        var stored = await scope.DbContext.ContentEntryRelations
            .AsNoTracking()
            .SingleAsync();

        stored.SourceEntryId.Should().Be(source.Id.Value);
        stored.TargetEntryId.Should().Be(target.Id.Value);
        stored.FieldName.Should().Be("relatedEntry");
    }

    [Fact]
    public async Task ContentEntryRelations_CascadeDeleteWhenSourceEntryRemoved()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var source = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "source");
        var target = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "target");
        await scope.ContentEntries.AddAsync(source);
        await scope.ContentEntries.AddAsync(target);
        await scope.UnitOfWork.SaveChangesAsync();

        scope.DbContext.ContentEntryRelations.Add(
            PersistenceTestDataFactory.CreateRelation(source.Id.Value, target.Id.Value));
        await scope.UnitOfWork.SaveChangesAsync();

        var sourceEntity = await scope.DbContext.ContentEntries.SingleAsync(entry => entry.Id == source.Id.Value);
        scope.DbContext.ContentEntries.Remove(sourceEntity);
        await scope.UnitOfWork.SaveChangesAsync();

        var relationCount = await scope.DbContext.ContentEntryRelations.CountAsync();
        relationCount.Should().Be(0);
    }
}
