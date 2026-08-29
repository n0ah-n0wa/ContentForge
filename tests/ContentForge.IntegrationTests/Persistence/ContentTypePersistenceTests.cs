namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class ContentTypePersistenceTests(PostgreSqlPersistenceFixture fixture)
    : PersistenceTestBase(fixture)
{
    [Fact]
    public async Task ContentType_PersistsAndReloadsWithDynamicFieldDefinitions()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType(
            name: "blogPost",
            slug: "blog-post");

        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentTypes.GetByIdAsync(contentType.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(FieldName.Create("blogPost"));
        loaded.Slug.Should().Be(Slug.Create("blog-post"));
        loaded.Fields.Should().HaveCount(2);

        var titleField = loaded.Fields.Single(field => field.Name == FieldName.Create("title"));
        titleField.FieldType.Should().Be(FieldType.Text);
        titleField.Configuration.IsRequired.Should().BeTrue();
        titleField.Configuration.MaxLength.Should().Be(200);

        var bodyField = loaded.Fields.Single(field => field.Name == FieldName.Create("body"));
        bodyField.FieldType.Should().Be(FieldType.LongText);
        bodyField.Configuration.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task ContentType_PersistsComplexFieldConfiguration()
    {
        await using var scope = CreatePersistenceScope();

        var articleType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(articleType);
        await scope.UnitOfWork.SaveChangesAsync();

        var relatedType = PersistenceTestDataFactory.CreateRelatedContentType(articleType.Id);
        await scope.ContentTypes.AddAsync(relatedType);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentTypes.GetByIdAsync(relatedType.Id);
        loaded.Should().NotBeNull();

        var relationField = loaded!.Fields.Single();
        relationField.FieldType.Should().Be(FieldType.Relation);
        relationField.Configuration.RelationTarget.Should().Be(articleType.Id);
        relationField.Configuration.RelationCardinality.Should().Be(RelationCardinality.ManyToOne);
    }

    [Fact]
    public async Task ContentType_UpdatePersistsFieldChanges()
    {
        await using var scope = CreatePersistenceScope();

        var contentType = PersistenceTestDataFactory.CreateArticleContentType();
        await scope.ContentTypes.AddAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var selectField = ContentTypeField.Create(
            FieldName.Create("category"),
            FieldType.Select,
            "Category",
            sortOrder: 3,
            FieldConfiguration.Create(options: ["News", "Guide"]));

        contentType.AddField(selectField, PersistenceTestConstants.ActorId, PersistenceTestConstants.BaseTimestamp.AddHours(1));
        await scope.ContentTypes.UpdateAsync(contentType);
        await scope.UnitOfWork.SaveChangesAsync();

        var loaded = await scope.ContentTypes.GetByIdAsync(contentType.Id);
        loaded!.Fields.Should().HaveCount(3);
        loaded.Fields.Single(field => field.Name == FieldName.Create("category")).Configuration.Options
            .Should().BeEquivalentTo(["News", "Guide"]);
    }
}
