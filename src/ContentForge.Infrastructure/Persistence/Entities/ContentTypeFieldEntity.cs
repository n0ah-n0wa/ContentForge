namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentTypeFieldEntity
{
    public Guid Id { get; set; }

    public Guid ContentTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string FieldType { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public int SortOrder { get; set; }

    public string ConfigurationJson { get; set; } = null!;

    public ContentTypeEntity ContentType { get; set; } = null!;
}
