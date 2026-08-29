namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentVersionEntity
{
    public Guid Id { get; set; }

    public Guid ContentEntryId { get; set; }

    public int VersionNumber { get; set; }

    public string SnapshotJson { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CreatedBy { get; set; }

    public string ChangeSummary { get; set; } = null!;

    public ContentEntryEntity ContentEntry { get; set; } = null!;
}
