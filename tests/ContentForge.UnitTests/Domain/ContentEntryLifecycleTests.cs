namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using FluentAssertions;

public sealed class ContentEntryLifecycleTests
{
    [Fact]
    public void WithdrawFromReview_TransitionsToDraft()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);

        entry.WithdrawFromReview(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp.AddMinutes(1));

        entry.Status.Should().Be(ContentStatus.Draft);
    }

    [Fact]
    public void WithdrawFromReview_FromDraft_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var action = () => entry.WithdrawFromReview(
            DomainTestData.User1,
            entry.ConcurrencyToken,
            DomainTestData.Timestamp);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void Archive_FromDraft_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var action = () => entry.Archive(
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Invalid archive",
            DomainTestData.Timestamp);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void RestoreVersion_WhenVersionNotInHistory_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        var foreignVersion = ContentVersion.Create(
            ContentEntryId.New(),
            VersionNumber.Initial,
            new ContentSnapshot(Slug.Create("other"), ContentData.Empty, ContentStatus.Draft),
            DomainTestData.User1,
            "Foreign version");

        var action = () => entry.RestoreVersion(
            foreignVersion,
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Restore",
            DomainTestData.Timestamp);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Publish_IsolatesPublishedSnapshotFromLaterDraftMutations()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);
        entry.Publish(contentType, DomainTestData.User1, entry.ConcurrencyToken, "Publish", DomainTestData.Timestamp.AddMinutes(1));

        var publishedTitle = entry.PublishedSnapshot!.Data.GetValue("title");

        entry.UpdateDraft(
            contentType,
            DomainTestData.CreateValidArticleData().WithValue("title", "Draft-only title"),
            Slug.Create("draft-slug"),
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Draft edit",
            DomainTestData.Timestamp.AddMinutes(2));

        entry.PublishedSnapshot!.Data.GetValue("title").Should().Be(publishedTitle);
        entry.DraftData.GetValue("title").Should().Be("Draft-only title");
    }
}

public sealed class ContentVersionImmutabilityTests
{
    [Fact]
    public void ContentVersion_HasNoPublicMutators()
    {
        var version = ContentVersion.Create(
            ContentEntryId.New(),
            VersionNumber.Initial,
            new ContentSnapshot(Slug.Create("slug"), ContentData.Empty, ContentStatus.Draft),
            DomainTestData.User1,
            "Created");

        typeof(ContentVersion)
            .GetProperties()
            .Should()
            .OnlyContain(property => property.CanWrite == false);
    }
}
