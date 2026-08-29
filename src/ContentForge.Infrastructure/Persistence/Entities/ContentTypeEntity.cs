namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentTypeEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public bool IsActive { get; set; }

    public int Version { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ContentTypeFieldEntity> Fields { get; set; } = [];

    public ICollection<ContentEntryEntity> Entries { get; set; } = [];
}
