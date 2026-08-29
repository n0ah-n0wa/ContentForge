namespace ContentForge.Domain.Content;

/// <summary>
/// Immutable historical version of a content entry.
/// </summary>
public sealed class ContentVersion
{
    private ContentVersion(
        Common.ContentVersionId id,
        Common.ContentEntryId contentEntryId,
        VersionNumber versionNumber,
        ContentSnapshot snapshot,
        DateTimeOffset createdAt,
        Common.UserId createdBy,
        string changeSummary)
    {
        Id = id;
        ContentEntryId = contentEntryId;
        VersionNumber = versionNumber;
        Snapshot = snapshot;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ChangeSummary = changeSummary;
    }

    public Common.ContentVersionId Id { get; }

    public Common.ContentEntryId ContentEntryId { get; }

    public VersionNumber VersionNumber { get; }

    public ContentSnapshot Snapshot { get; }

    public DateTimeOffset CreatedAt { get; }

    public Common.UserId CreatedBy { get; }

    public string ChangeSummary { get; }

    public static ContentVersion Create(
        Common.ContentEntryId contentEntryId,
        VersionNumber versionNumber,
        ContentSnapshot snapshot,
        Common.UserId createdBy,
        string changeSummary,
        DateTimeOffset? createdAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        return new ContentVersion(
            Common.ContentVersionId.New(),
            contentEntryId,
            versionNumber,
            snapshot,
            createdAt ?? DateTimeOffset.UtcNow,
            createdBy,
            changeSummary.Trim());
    }

    internal static ContentVersion Restore(
        Common.ContentVersionId id,
        Common.ContentEntryId contentEntryId,
        VersionNumber versionNumber,
        ContentSnapshot snapshot,
        DateTimeOffset createdAt,
        Common.UserId createdBy,
        string changeSummary) =>
        new(id, contentEntryId, versionNumber, snapshot, createdAt, createdBy, changeSummary);
}
