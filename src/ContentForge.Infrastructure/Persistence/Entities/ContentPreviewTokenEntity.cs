namespace ContentForge.Infrastructure.Persistence.Entities;

public sealed class ContentPreviewTokenEntity
{
    public Guid Id { get; set; }

    public Guid ContentEntryId { get; set; }

    public string TokenHash { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
