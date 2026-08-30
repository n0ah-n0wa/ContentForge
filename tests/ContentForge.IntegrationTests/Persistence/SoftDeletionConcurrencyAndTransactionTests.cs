namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class SoftDeletionPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntry_SoftDeleteExcludesFromDefaultQueries()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "soft-deleted");
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        loaded!.SoftDelete(PersistenceTestConstants.ActorId, loaded.ConcurrencyToken, PersistenceTestConstants.BaseTimestamp.AddMinutes(1));
        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var bySlug = await scope.ContentEntries.GetBySlugAsync(contentType.Id, Slug.Create("soft-deleted"));
        bySlug.Should().BeNull();

        var exists = await scope.ContentEntries.ExistsBySlugAsync(contentType.Id, Slug.Create("soft-deleted"));
        exists.Should().BeFalse();

        var stillStored = await scope.ContentEntries.GetByIdAsync(entry.Id);
        stillStored.Should().NotBeNull();
        stillStored!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ContentEntry_SoftDeleteAllowsSlugReuse()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var deletedEntry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "reusable-slug");
        await scope.ContentEntries.AddAsync(deletedEntry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(deletedEntry.Id);
        loaded!.SoftDelete(PersistenceTestConstants.ActorId, loaded.ConcurrencyToken, PersistenceTestConstants.BaseTimestamp.AddMinutes(1));
        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var replacement = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "reusable-slug");
        await scope.ContentEntries.AddAsync(replacement);

        var action = async () => await scope.UnitOfWork.SaveChangesAsync();
        await action.Should().NotThrowAsync();
    }
}

public sealed class ConcurrencyPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentEntry_OptimisticConcurrencyConflict_IsDetectedWhenClientBIsStale()
    {
        var contentType = PersistenceTestDataFactory.CreateArticleContentType();

        ContentEntryId entryId;
        ConcurrencyToken originalToken;

        await using (var seedScope = CreatePersistenceScope())
        {
            await seedScope.ContentTypes.AddAsync(contentType);
            await seedScope.UnitOfWork.SaveChangesAsync();

            var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "concurrency-target");
            await seedScope.ContentEntries.AddAsync(entry);
            await seedScope.UnitOfWork.SaveChangesAsync();

            entryId = entry.Id;
            originalToken = entry.ConcurrencyToken;
        }

        ContentEntry clientAEntry;
        ContentEntry clientBEntry;

        await using (var scopeA = CreatePersistenceScope())
        {
            clientAEntry = (await scopeA.ContentEntries.GetByIdAsync(entryId))!;
        }

        await using (var scopeB = CreatePersistenceScope())
        {
            clientBEntry = (await scopeB.ContentEntries.GetByIdAsync(entryId))!;
        }

        clientAEntry.ConcurrencyToken.Should().Be(originalToken);
        clientBEntry.ConcurrencyToken.Should().Be(originalToken);

        await using (var scopeA = CreatePersistenceScope())
        {
            var contentTypeForUpdate = (await scopeA.ContentTypes.GetByIdAsync(contentType.Id))!;
            var entryA = (await scopeA.ContentEntries.GetByIdAsync(entryId))!;

            entryA.UpdateDraft(
                contentTypeForUpdate,
                entryA.DraftData.WithValue("title", "Client A update"),
                entryA.Slug,
                PersistenceTestConstants.ActorId,
                originalToken,
                "Updated by client A",
                PersistenceTestConstants.BaseTimestamp.AddMinutes(10));

            await scopeA.ContentEntries.UpdateAsync(entryA);
            await scopeA.UnitOfWork.SaveChangesAsync();
        }

        await using (var scopeB = CreatePersistenceScope())
        {
            var staleEntity = await scopeB.DbContext.ContentEntries
                .AsNoTracking()
                .SingleAsync(entry => entry.Id == entryId.Value);

            scopeB.DbContext.Attach(staleEntity);
            var trackedEntry = scopeB.DbContext.Entry(staleEntity);
            trackedEntry.Property(entry => entry.ConcurrencyToken).OriginalValue = originalToken.Value;
            trackedEntry.Property(entry => entry.ConcurrencyToken).CurrentValue = originalToken.Value + 1;
            staleEntity.IsDeleted = true;
            trackedEntry.Property(entry => entry.IsDeleted).IsModified = true;

            var action = async () => await scopeB.UnitOfWork.SaveChangesAsync();
            await action.Should().ThrowAsync<ConcurrencyConflictException>();
        }

        await using var verifyScope = CreatePersistenceScope();
        var persisted = (await verifyScope.ContentEntries.GetByIdAsync(entryId))!;
        persisted.DraftData.GetValue("title").Should().Be("Client A update");
        persisted.ConcurrencyToken.Value.Should().Be(originalToken.Value + 1);
    }

    [Fact]
    public async Task ContentEntry_StaleInMemoryAggregate_DoesNotOverwriteCommittedUpdate()
    {
        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        ContentEntryId entryId;
        ConcurrencyToken originalToken;

        await using (var seedScope = CreatePersistenceScope())
        {
            await seedScope.ContentTypes.AddAsync(contentType);
            await seedScope.UnitOfWork.SaveChangesAsync();

            var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "lost-update-guard");
            await seedScope.ContentEntries.AddAsync(entry);
            await seedScope.UnitOfWork.SaveChangesAsync();
            entryId = entry.Id;
            originalToken = entry.ConcurrencyToken;
        }

        ContentEntry staleClient;
        await using (var loadScope = CreatePersistenceScope())
        {
            staleClient = (await loadScope.ContentEntries.GetByIdAsync(entryId))!;
        }

        await using (var freshScope = CreatePersistenceScope())
        {
            var type = (await freshScope.ContentTypes.GetByIdAsync(contentType.Id))!;
            var fresh = (await freshScope.ContentEntries.GetByIdAsync(entryId))!;
            fresh.UpdateDraft(
                type,
                fresh.DraftData.WithValue("title", "Committed title"),
                fresh.Slug,
                PersistenceTestConstants.ActorId,
                originalToken,
                "First writer",
                PersistenceTestConstants.BaseTimestamp.AddMinutes(1));
            await freshScope.ContentEntries.UpdateAsync(fresh);
            await freshScope.UnitOfWork.SaveChangesAsync();
        }

        await using (var staleScope = CreatePersistenceScope())
        {
            var type = (await staleScope.ContentTypes.GetByIdAsync(contentType.Id))!;
            staleClient.UpdateDraft(
                type,
                staleClient.DraftData.WithValue("title", "Stale overwrite"),
                staleClient.Slug,
                PersistenceTestConstants.ActorId,
                originalToken,
                "Second writer",
                PersistenceTestConstants.BaseTimestamp.AddMinutes(2));

            var action = async () =>
            {
                await staleScope.ContentEntries.UpdateAsync(staleClient);
                await staleScope.UnitOfWork.SaveChangesAsync();
            };

            await action.Should().ThrowAsync<ConcurrencyConflictException>()
                .Where(exception => exception.ExpectedVersion == originalToken.Value
                    && exception.ActualVersion == originalToken.Value + 1);
        }

        await using var verifyScope = CreatePersistenceScope();
        var persisted = (await verifyScope.ContentEntries.GetByIdAsync(entryId))!;
        persisted.DraftData.GetValue("title").Should().Be("Committed title");
        persisted.ConcurrencyToken.Value.Should().Be(originalToken.Value + 1);
    }

    [Fact]
    public async Task ContentEntry_DomainConcurrencyGuard_RejectsStaleExpectedTokenAfterReload()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id, slug: "domain-concurrency");
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = (await scope.ContentEntries.GetByIdAsync(entry.Id))!;
        var staleToken = loaded.ConcurrencyToken;

        loaded.UpdateDraft(
            contentType,
            loaded.DraftData.WithValue("title", "Fresh update"),
            loaded.Slug,
            PersistenceTestConstants.ActorId,
            staleToken,
            "Committed update",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(1));

        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var reloaded = (await scope.ContentEntries.GetByIdAsync(entry.Id))!;
        var action = () => reloaded.UpdateDraft(
            contentType,
            reloaded.DraftData.WithValue("title", "Should fail"),
            reloaded.Slug,
            PersistenceTestConstants.ActorId,
            staleToken,
            "Stale expected token",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(2));

        action.Should().Throw<ConcurrencyConflictException>();
    }
}

public sealed class TransactionPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ExplicitTransaction_RollsBackUncommittedChanges()
    {
        var contentType = PersistenceTestDataFactory.CreateArticleContentType(name: "txArticle", slug: "tx-article");

        await using (var writeScope = CreatePersistenceScope())
        {
            await using var transaction = await writeScope.DbContext.Database.BeginTransactionAsync();
            await writeScope.ContentTypes.AddAsync(contentType);
            await writeScope.UnitOfWork.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verifyScope = CreatePersistenceScope();
        var loaded = await verifyScope.ContentTypes.GetBySlugAsync(Slug.Create("tx-article"));
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ExplicitTransaction_CommitsChangesAtomically()
    {
        var contentType = PersistenceTestDataFactory.CreateArticleContentType(name: "txCommit", slug: "tx-commit");

        await using (var writeScope = CreatePersistenceScope())
        {
            await using var transaction = await writeScope.DbContext.Database.BeginTransactionAsync();
            await writeScope.ContentTypes.AddAsync(contentType);
            await writeScope.UnitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var verifyScope = CreatePersistenceScope();
        var loaded = await verifyScope.ContentTypes.GetBySlugAsync(Slug.Create("tx-commit"));
        loaded.Should().NotBeNull();
    }
}
