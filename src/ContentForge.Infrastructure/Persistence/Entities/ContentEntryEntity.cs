namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentEntryEntity
{
    public Guid Id { get; set; }

    public Guid ContentTypeId { get; set; }

    public string Slug { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string DraftDataJson { get; set; } = null!;

    public string? PublishedSnapshotJson { get; set; }

    public int CurrentVersion { get; set; }

    public long ConcurrencyToken { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid? PublishedBy { get; set; }

    public DateTimeOffset? ScheduledPublishAt { get; set; }

    public DateTimeOffset? ScheduledUnpublishAt { get; set; }

    public bool IsDeleted { get; set; }

    public ContentTypeEntity ContentType { get; set; } = null!;

    public ICollection<ContentVersionEntity> Versions { get; set; } = [];

    public ICollection<ContentEntryRelationEntity> OutgoingRelations { get; set; } = [];

    public ICollection<ContentEntryRelationEntity> IncomingRelations { get; set; } = [];
}
