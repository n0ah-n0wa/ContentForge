namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Media;
using FluentAssertions;

public sealed class AuthorizationRulesTests
{
    [Fact]
    public void Viewer_DoesNotHavePublishPermission()
    {
        DefaultRoleDefinitions.Viewer.HasPermission(Permissions.ContentPublish).Should().BeFalse();
    }

    [Fact]
    public void Editor_HasPublishPermission()
    {
        DefaultRoleDefinitions.Editor.HasPermission(Permissions.ContentPublish).Should().BeTrue();
    }

    [Fact]
    public void Author_CannotModifyContentOwnedByAnotherUser()
    {
        var action = () => AuthorizationRules.EnsureCanModifyContent(
            DefaultRoleDefinitions.Author,
            DomainTestData.User1,
            DomainTestData.User2);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void EnsureAllowed_WhenMissingPermission_Throws()
    {
        var action = () => AuthorizationRules.EnsureAllowed(
            DefaultRoleDefinitions.Viewer,
            Permissions.ContentCreate);

        action.Should().Throw<InvalidOperationDomainException>();
    }
}

public sealed class AuditLogEntryTests
{
    [Fact]
    public void Create_InitializesImmutableEntry()
    {
        var entry = AuditLogEntry.Create(
            AuditAction.ContentPublished,
            "ContentEntry",
            Guid.NewGuid().ToString(),
            DomainTestData.User1,
            metadata: "{\"slug\":\"hello-world\"}");

        entry.Action.Should().Be(AuditAction.ContentPublished);
        entry.EntityType.Should().Be("ContentEntry");
        entry.Metadata.Should().Contain("hello-world");
    }
}

public sealed class MediaAssetTests
{
    [Fact]
    public void Create_GeneratesSafeStorageKey()
    {
        var media = MediaAsset.Create(
            "cover.jpg",
            "My Cover.jpg",
            "image/jpeg",
            1024,
            StorageKey.Create(Guid.NewGuid(), ".jpg"),
            DomainTestData.User1);

        media.StorageKey.Value.Should().StartWith("media/");
        media.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithPathTraversalInFileName_Throws()
    {
        var action = () => MediaAsset.Create(
            "../secrets.jpg",
            "secrets.jpg",
            "image/jpeg",
            1024,
            StorageKey.Create(Guid.NewGuid(), ".jpg"),
            DomainTestData.User1);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void MarkDeleted_PreventsMetadataUpdates()
    {
        var media = MediaAsset.Create(
            "cover.jpg",
            "My Cover.jpg",
            "image/jpeg",
            1024,
            StorageKey.Create(Guid.NewGuid(), ".jpg"),
            DomainTestData.User1);

        media.MarkDeleted();

        var action = () => media.UpdateMetadata("alt", "title", "description");

        action.Should().Throw<InvalidOperationDomainException>();
    }
}
