namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using FluentAssertions;

public sealed class ContentDataValidatorTests
{
    [Fact]
    public void Validate_WithValidData_DoesNotThrow()
    {
        var contentType = DomainTestData.CreateArticleType();
        var data = DomainTestData.CreateValidArticleData();

        var action = () => ContentDataValidator.Validate(contentType, data);

        action.Should().NotThrow();
    }

    [Fact]
    public void CollectValidationErrors_WhenRequiredFieldMissing_ReturnsError()
    {
        var contentType = DomainTestData.CreateArticleType();
        var data = ContentData.FromDictionary(new Dictionary<string, object?>
        {
            ["body"] = "<p>Body</p>",
        });

        var errors = ContentDataValidator.CollectValidationErrors(contentType, data);

        errors.Should().Contain(error => error.Field == "title");
    }

    [Fact]
    public void CollectValidationErrors_WhenUnexpectedFieldPresent_ReturnsError()
    {
        var contentType = DomainTestData.CreateArticleType();
        var data = DomainTestData.CreateValidArticleData().WithValue("unexpected", "value");

        var errors = ContentDataValidator.CollectValidationErrors(contentType, data);

        errors.Should().Contain(error => error.Field == "unexpected");
    }

    [Fact]
    public void CollectValidationErrors_WhenStringExceedsMaxLength_ReturnsError()
    {
        var contentType = DomainTestData.CreateArticleType();
        var data = DomainTestData.CreateValidArticleData().WithValue("title", new string('a', 201));

        var errors = ContentDataValidator.CollectValidationErrors(contentType, data);

        errors.Should().Contain(error => error.Field == "title" && error.Message.Contains("Maximum length"));
    }
}

public sealed class ContentVersionComparerTests
{
    [Fact]
    public void Compare_DetectsFieldAndSlugChanges()
    {
        var left = new ContentSnapshot(
            Slug.Create("old-slug"),
            ContentData.FromDictionary(new Dictionary<string, object?> { ["title"] = "Old" }),
            ContentStatus.Draft);

        var right = new ContentSnapshot(
            Slug.Create("new-slug"),
            ContentData.FromDictionary(new Dictionary<string, object?> { ["title"] = "New" }),
            ContentStatus.Published);

        var changes = ContentVersionComparer.Compare(left, right);

        changes.Should().Contain(change => change.FieldName == "_slug");
        changes.Should().Contain(change => change.FieldName == "title");
        changes.Should().Contain(change => change.FieldName == "_status");
    }

    [Fact]
    public void Compare_DetectsCollectionChanges()
    {
        var left = new ContentSnapshot(
            Slug.Create("slug"),
            ContentData.FromDictionary(new Dictionary<string, object?> { ["tags"] = new List<object?> { "a", "b" } }),
            ContentStatus.Draft);

        var right = new ContentSnapshot(
            Slug.Create("slug"),
            ContentData.FromDictionary(new Dictionary<string, object?> { ["tags"] = new List<object?> { "a", "c" } }),
            ContentStatus.Draft);

        var changes = ContentVersionComparer.Compare(left, right);

        changes.Should().ContainSingle(change => change.FieldName == "tags");
    }
}
