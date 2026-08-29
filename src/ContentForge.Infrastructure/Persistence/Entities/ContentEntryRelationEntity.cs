namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentEntryRelationEntity
{
    public Guid Id { get; set; }

    public Guid SourceEntryId { get; set; }

    public Guid TargetEntryId { get; set; }

    public string FieldName { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public ContentEntryEntity SourceEntry { get; set; } = null!;

    public ContentEntryEntity TargetEntry { get; set; } = null!;
}
