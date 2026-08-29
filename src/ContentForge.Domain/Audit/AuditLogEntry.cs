namespace ContentForge.Domain.Audit;

using ContentForge.Domain.Common;

/// <summary>
/// Auditable actions recorded by the system.
/// </summary>
public enum AuditAction
{
    UserCreated,
    UserDisabled,
    UserRoleChanged,
    LoginSucceeded,
    LoginFailed,
    ContentCreated,
    ContentUpdated,
    ContentSubmittedForReview,
    ContentWithdrawnFromReview,
    ContentPublished,
    ContentUnpublished,
    ContentArchived,
    ContentRestored,
    ContentDeleted,
    ContentTypeCreated,
    ContentTypeUpdated,
    MediaUploaded,
    MediaDeleted,
}

/// <summary>
/// Immutable audit log entry.
/// </summary>
public sealed class AuditLogEntry
{
    private AuditLogEntry(
        AuditLogId id,
        DateTimeOffset timestamp,
        UserId? userId,
        AuditAction action,
        string entityType,
        string entityId,
        string? metadata,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        Timestamp = timestamp;
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Metadata = metadata;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public AuditLogId Id { get; }

    public DateTimeOffset Timestamp { get; }

    public UserId? UserId { get; }

    public AuditAction Action { get; }

    public string EntityType { get; }

    public string EntityId { get; }

    public string? Metadata { get; }

    public string? IpAddress { get; }

    public string? UserAgent { get; }

    public static AuditLogEntry Create(
        AuditAction action,
        string entityType,
        string entityId,
        UserId? userId = null,
        string? metadata = null,
        string? ipAddress = null,
        string? userAgent = null,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        return new AuditLogEntry(
            AuditLogId.New(),
            timestamp ?? DateTimeOffset.UtcNow,
            userId,
            action,
            entityType.Trim(),
            entityId.Trim(),
            metadata,
            ipAddress,
            userAgent);
    }
}
