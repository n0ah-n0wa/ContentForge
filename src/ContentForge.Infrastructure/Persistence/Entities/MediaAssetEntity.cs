namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class MediaAssetEntity
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long Size { get; set; }

    public string StorageKey { get; set; } = null!;

    public string? Url { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? AltText { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public Guid UploadedBy { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public bool IsDeleted { get; set; }
}
