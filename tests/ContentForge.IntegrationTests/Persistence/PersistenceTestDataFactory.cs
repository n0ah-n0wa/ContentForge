namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.Infrastructure.Persistence.Entities;

internal static class PersistenceTestDataFactory
{
    internal static ContentType CreateArticleContentType(
        string name = "article",
        string slug = "article",
        IEnumerable<ContentTypeField>? fields = null)
    {
        var fieldList = fields?.ToList()
            ??
            [
                ContentTypeField.Create(
                    FieldName.Create("title"),
                    FieldType.Text,
                    "Title",
                    sortOrder: 1,
                    FieldConfiguration.Create(isRequired: true, maxLength: 200)),
                ContentTypeField.Create(
                    FieldName.Create("body"),
                    FieldType.LongText,
                    "Body",
                    sortOrder: 2,
                    FieldConfiguration.Create(isRequired: false)),
            ];

        return ContentType.Create(
            FieldName.Create(name),
            "Article",
            Slug.Create(slug),
            PersistenceTestConstants.ActorId,
            description: "Test article content type",
            fields: fieldList,
            createdAt: PersistenceTestConstants.BaseTimestamp);
    }

    internal static ContentType CreateRelatedContentType(ContentTypeId relationTarget) =>
        ContentType.Create(
            FieldName.Create("relatedItem"),
            "Related Item",
            Slug.Create("related-item"),
            PersistenceTestConstants.ActorId,
            fields:
            [
                ContentTypeField.Create(
                    FieldName.Create("relatedEntry"),
                    FieldType.Relation,
                    "Related Entry",
                    sortOrder: 1,
                    FieldConfiguration.Create(
                        relationTarget: relationTarget,
                        relationCardinality: RelationCardinality.ManyToOne)),
            ],
            createdAt: PersistenceTestConstants.BaseTimestamp);

    internal static ContentEntry CreateDraftEntry(
        ContentTypeId contentTypeId,
        string slug = "first-article",
        ContentData? data = null) =>
        ContentEntry.Create(
            contentTypeId,
            Slug.Create(slug),
            PersistenceTestConstants.ActorId,
            data ?? ContentData.FromDictionary(new Dictionary<string, object?>
            {
                ["title"] = "Hello World",
                ["body"] = "Draft body",
            }),
            createdAt: PersistenceTestConstants.BaseTimestamp);

    internal static ContentEntryRelationEntity CreateRelation(
        Guid sourceEntryId,
        Guid targetEntryId,
        string fieldName = "relatedEntry") => new()
        {
            Id = Guid.NewGuid(),
            SourceEntryId = sourceEntryId,
            TargetEntryId = targetEntryId,
            FieldName = fieldName,
            CreatedAt = PersistenceTestConstants.BaseTimestamp,
        };
}
