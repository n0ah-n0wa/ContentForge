namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

internal static class DomainTestData
{
    internal static UserId User1 { get; } = UserId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    internal static UserId User2 { get; } = UserId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    internal static DateTimeOffset Timestamp { get; } = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    internal static ContentType CreateArticleType()
    {
        var contentType = ContentType.Create(
            FieldName.Create("Article"),
            "Article",
            Slug.Create("article"),
            User1,
            "Article content type",
            createdAt: Timestamp);

        contentType.AddField(
            ContentTypeField.Create(
                FieldName.Create("title"),
                FieldType.Text,
                "Title",
                1,
                FieldConfiguration.Create(isRequired: true, maxLength: 200)),
            User1,
            Timestamp);

        contentType.AddField(
            ContentTypeField.Create(
                FieldName.Create("body"),
                FieldType.RichText,
                "Body",
                2,
                FieldConfiguration.Create(isRequired: true)),
            User1,
            Timestamp);

        contentType.AddField(
            ContentTypeField.Create(
                FieldName.Create("publishedAt"),
                FieldType.DateTime,
                "Published At",
                3,
                FieldConfiguration.Create()),
            User1,
            Timestamp);

        return contentType;
    }

    internal static ContentData CreateValidArticleData() =>
        ContentData.FromDictionary(new Dictionary<string, object?>
        {
            ["title"] = "Hello World",
            ["body"] = "<p>Body</p>",
            ["publishedAt"] = Timestamp,
        });

    internal static ContentEntry CreateDraftEntry(ContentType contentType) =>
        ContentEntry.Create(contentType.Id, Slug.Create("hello-world"), User1, CreateValidArticleData(), Timestamp);
}
