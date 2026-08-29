namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Content;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ContentVersionPersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentVersion_AppendsImmutableHistoryOnDraftUpdate()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id);
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        loaded!.UpdateDraft(
            contentType,
            ContentData.FromDictionary(new Dictionary<string, object?>
            {
                ["title"] = "Updated title",
                ["body"] = "Updated body",
            }),
            loaded.Slug,
            PersistenceTestConstants.ActorId,
            loaded.ConcurrencyToken,
            "Initial draft revision",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(5));

        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var reloaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        reloaded!.Versions.Should().HaveCount(1);
        reloaded.CurrentVersion.Value.Should().Be(1);
        reloaded.Versions[0].ChangeSummary.Should().Be("Initial draft revision");
        reloaded.Versions[0].Snapshot.Data.GetValue("title").Should().Be("Updated title");
    }

    [Fact]
    public async Task ContentVersion_DoesNotMutateExistingVersionRows()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id);
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        var originalVersionId = loaded!.UpdateDraft(
            contentType,
            loaded.DraftData,
            loaded.Slug,
            PersistenceTestConstants.ActorId,
            loaded.ConcurrencyToken,
            "Version 1",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(1)).Id;

        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        loaded!.UpdateDraft(
            contentType,
            loaded.DraftData.WithValue("title", "Second update"),
            loaded.Slug,
            PersistenceTestConstants.ActorId,
            loaded.ConcurrencyToken,
            "Version 2",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(2));

        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var reloaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        reloaded!.Versions.Should().HaveCount(2);
        reloaded.Versions.Single(version => version.Id == originalVersionId).ChangeSummary.Should().Be("Version 1");
        reloaded.Versions.Should().OnlyContain(version => version.ContentEntryId == entry.Id);
    }

    [Fact]
    public async Task ContentVersion_DirectModificationIsRejectedByDbContext()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var entry = PersistenceTestDataFactory.CreateDraftEntry(contentType.Id);
        await scope.ContentEntries.AddAsync(entry);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentEntries.GetByIdAsync(entry.Id);
        loaded!.UpdateDraft(
            contentType,
            loaded.DraftData,
            loaded.Slug,
            PersistenceTestConstants.ActorId,
            loaded.ConcurrencyToken,
            "Version 1",
            PersistenceTestConstants.BaseTimestamp.AddMinutes(1));

        await scope.ContentEntries.UpdateAsync(loaded);
        await scope.UnitOfWork.SaveChangesAsync();

        var version = await scope.DbContext.ContentVersions.SingleAsync();
        version.ChangeSummary = "Tampered summary";
        scope.DbContext.Entry(version).Property(v => v.ChangeSummary).IsModified = true;

        var action = async () => await scope.UnitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }
}
