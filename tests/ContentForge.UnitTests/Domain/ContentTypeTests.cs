namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;
using FluentAssertions;

public sealed class ContentTypeTests
{
    [Fact]
    public void Create_InitializesActiveContentType()
    {
        var contentType = DomainTestData.CreateArticleType();

        contentType.IsActive.Should().BeTrue();
        contentType.Fields.Should().HaveCount(3);
        contentType.Version.Should().Be(4);
    }

    [Fact]
    public void AddField_WithDuplicateName_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();
        var duplicate = ContentTypeField.Create(
            FieldName.Create("title"),
            FieldType.Text,
            "Duplicate",
            99,
            FieldConfiguration.Create());

        var action = () => contentType.AddField(duplicate, DomainTestData.User1, DomainTestData.Timestamp);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void RemoveField_WithoutConfirmation_Throws()
    {
        var contentType = DomainTestData.CreateArticleType();

        var action = () => contentType.RemoveField(FieldName.Create("title"), confirmed: false, DomainTestData.User1, DomainTestData.Timestamp);

        action.Should().Throw<InvalidOperationDomainException>();
    }

    [Fact]
    public void EnsureCanDelete_WithDependentEntriesWithoutConfirmation_Throws()
    {
        var action = () => ContentType.EnsureCanDelete(hasDependentEntries: true, confirmedSafeDeletion: false);

        action.Should().Throw<InvalidOperationDomainException>();
    }
}

public sealed class ContentTypeSchemaEvolutionTests
{
    [Fact]
    public void IsSafeAddition_WhenOptionalField_ReturnsTrue()
    {
        var field = ContentTypeField.Create(
            FieldName.Create("subtitle"),
            FieldType.Text,
            "Subtitle",
            4,
            FieldConfiguration.Create(isRequired: false));

        ContentTypeSchemaEvolution.IsSafeAddition(field).Should().BeTrue();
    }

    [Fact]
    public void EnsureValidationChange_WhenFieldBecomesRequiredWithoutConfirmation_Throws()
    {
        var field = ContentTypeField.Create(
            FieldName.Create("subtitle"),
            FieldType.Text,
            "Subtitle",
            4,
            FieldConfiguration.Create(isRequired: false));

        var action = () => ContentTypeSchemaEvolution.EnsureValidationChangeAllowed(
            field,
            FieldConfiguration.Create(isRequired: true),
            confirmed: false);

        action.Should().Throw<InvalidOperationDomainException>();
    }
}

public sealed class ContentTypeFieldTests
{
    [Fact]
    public void Create_SelectFieldWithoutOptions_Throws()
    {
        var action = () => ContentTypeField.Create(
            FieldName.Create("category"),
            FieldType.Select,
            "Category",
            1,
            FieldConfiguration.Create());

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_RelationFieldWithoutTarget_Throws()
    {
        var action = () => ContentTypeField.Create(
            FieldName.Create("author"),
            FieldType.Relation,
            "Author",
            1,
            FieldConfiguration.Create(relationCardinality: RelationCardinality.ManyToOne));

        action.Should().Throw<DomainValidationException>();
    }
}
