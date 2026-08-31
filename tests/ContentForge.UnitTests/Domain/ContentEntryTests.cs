namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using FluentAssertions;

public sealed class ContentEntryTests
{
    [Fact]
    public void Publish_FromInReview_CreatesVersionAndPublishedSnapshot()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(contentType, DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);

        var version = entry.Publish(
            contentType,
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Initial publish",
            DomainTestData.Timestamp.AddMinutes(1));

        entry.Status.Should().Be(ContentStatus.Published);
        entry.HasPublishedRepresentation.Should().BeTrue();
        entry.PublishedSnapshot.Should().NotBeNull();
        entry.PublishedAt.Should().NotBeNull();
        entry.PublishedBy.Should().Be(DomainTestData.User1);
        entry.Versions.Should().HaveCount(3);
        version.VersionNumber.Should().Be(new VersionNumber(3));
    }

    [Fact]
    public void Publish_FromDraft_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var action = () => entry.Publish(
            contentType,
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Invalid publish",
            DomainTestData.Timestamp);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void UpdateDraft_WhenPublished_DoesNotModifyPublishedSnapshot()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = PublishEntry(contentType);
        var publishedSnapshot = entry.PublishedSnapshot;
        var updatedData = DomainTestData.CreateValidArticleData().WithValue("title", "Updated title");

        entry.UpdateDraft(
            contentType,
            updatedData,
            Slug.Create("updated-slug"),
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Draft update",
            DomainTestData.Timestamp.AddMinutes(2));

        entry.Status.Should().Be(ContentStatus.Draft);
        entry.DraftData.GetValue("title").Should().Be("Updated title");
        entry.PublishedSnapshot.Should().Be(publishedSnapshot);
        entry.HasPublishedRepresentation.Should().BeTrue();
    }

    [Fact]
    public void UpdateDraft_WithStaleConcurrencyToken_ThrowsConflict()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        var staleToken = entry.ConcurrencyToken;

        entry.SubmitForReview(contentType, DomainTestData.User1, staleToken, DomainTestData.Timestamp);

        var action = () => entry.UpdateDraft(
            contentType,
            DomainTestData.CreateValidArticleData(),
            Slug.Create("hello-world"),
            DomainTestData.User1,
            staleToken,
            "Conflict",
            DomainTestData.Timestamp);

        action.Should().Throw<ConcurrencyConflictException>()
            .Which.ExpectedVersion.Should().Be(staleToken.Value);
    }

    [Fact]
    public void Unpublish_RemovesPublishedSnapshotButRetainsVersions()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = PublishEntry(contentType);
        var versionCount = entry.Versions.Count;

        entry.Unpublish(
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Unpublish",
            DomainTestData.Timestamp.AddMinutes(3));

        entry.Status.Should().Be(ContentStatus.Draft);
        entry.PublishedSnapshot.Should().BeNull();
        entry.PublishedAt.Should().BeNull();
        entry.PublishedBy.Should().BeNull();
        entry.HasPublishedRepresentation.Should().BeFalse();
        entry.Versions.Should().HaveCount(versionCount + 1);
    }

    [Fact]
    public void RestoreVersion_CreatesNewVersionWithoutMutatingHistoricalVersion()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        var firstVersion = entry.UpdateDraft(
            contentType,
            DomainTestData.CreateValidArticleData(),
            Slug.Create("hello-world"),
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Initial save",
            DomainTestData.Timestamp);

        entry.UpdateDraft(
            contentType,
            DomainTestData.CreateValidArticleData().WithValue("title", "Changed"),
            Slug.Create("changed-slug"),
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Changed title",
            DomainTestData.Timestamp.AddMinutes(1));

        var restoreVersion = entry.RestoreVersion(
            contentType,
            firstVersion,
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Restore first version",
            DomainTestData.Timestamp.AddMinutes(2));

        firstVersion.Snapshot.Data.GetValue("title").Should().Be("Hello World");
        entry.DraftData.GetValue("title").Should().Be("Hello World");
        restoreVersion.VersionNumber.Value.Should().BeGreaterThan(firstVersion.VersionNumber.Value);
        entry.Versions.Should().HaveCount(4);
    }

    [Fact]
    public void Create_RecordsInitialVersionWithCompleteSnapshot()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        entry.Versions.Should().ContainSingle();
        entry.CurrentVersion.Should().Be(VersionNumber.Initial);
        var version = entry.Versions[0];
        version.VersionNumber.Should().Be(VersionNumber.Initial);
        version.Snapshot.Slug.Should().Be(entry.Slug);
        version.Snapshot.Status.Should().Be(ContentStatus.Draft);
        version.Snapshot.Data.GetValue("title").Should().Be("Hello World");
        version.ChangeSummary.Should().Be("Created");
    }

    [Fact]
    public void Mutations_RecordSequentialVersionNumbers()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        entry.UpdateDraft(
            contentType,
            DomainTestData.CreateValidArticleData().WithValue("title", "Second"),
            entry.Slug,
            DomainTestData.User1,
            entry.ConcurrencyToken,
            "Edit",
            DomainTestData.Timestamp.AddMinutes(1));
        entry.SubmitForReview(contentType, DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp.AddMinutes(2));
        entry.Publish(contentType, DomainTestData.User1, entry.ConcurrencyToken, "Publish", DomainTestData.Timestamp.AddMinutes(3));

        entry.Versions.Select(version => version.VersionNumber.Value).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void SoftDelete_PreventsFurtherModification()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        entry.SoftDelete(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);

        var action = () => entry.SubmitForReview(contentType, DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void SetPublishingSchedule_WithPastPublishAt_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var action = () => entry.SetPublishingSchedule(
            DomainTestData.Timestamp.AddMinutes(-1),
            null,
            DomainTestData.User1,
            DomainTestData.Timestamp);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void SetPublishingSchedule_WithUnpublishBeforePublish_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        var publishAt = DomainTestData.Timestamp.AddHours(2);
        var unpublishAt = DomainTestData.Timestamp.AddHours(1);

        var action = () => entry.SetPublishingSchedule(
            publishAt,
            unpublishAt,
            DomainTestData.User1,
            DomainTestData.Timestamp);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void PublishScheduled_FromDraft_PublishesWithoutManualReview()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SetPublishingSchedule(
            DomainTestData.Timestamp.AddHours(1),
            null,
            DomainTestData.User1,
            DomainTestData.Timestamp);

        entry.PublishScheduled(contentType, DomainTestData.User1, DomainTestData.Timestamp.AddHours(1));

        entry.Status.Should().Be(ContentStatus.Published);
        entry.ScheduledPublishAt.Should().BeNull();
        entry.HasPublishedRepresentation.Should().BeTrue();
    }

    private static ContentEntry PublishEntry(ContentType contentType)
    {
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(contentType, DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);
        entry.Publish(contentType, DomainTestData.User1, entry.ConcurrencyToken, "Publish", DomainTestData.Timestamp.AddMinutes(1));
        return entry;
    }
}
