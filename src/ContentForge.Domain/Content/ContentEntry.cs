namespace ContentForge.Domain.Content;

/// <summary>
/// Aggregate root representing an instance of a content type.
/// </summary>
public sealed class ContentEntry
{
    private readonly List<ContentVersion> _versions = [];

    private ContentEntry(
        Common.ContentEntryId id,
        Common.ContentTypeId contentTypeId,
        Common.Slug slug,
        ContentStatus status,
        ContentData draftData,
        ContentSnapshot? publishedSnapshot,
        VersionNumber currentVersion,
        Common.ConcurrencyToken concurrencyToken,
        Common.UserId createdBy,
        Common.UserId updatedBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? publishedAt,
        Common.UserId? publishedBy,
        bool isDeleted)
    {
        Id = id;
        ContentTypeId = contentTypeId;
        Slug = slug;
        Status = status;
        DraftData = draftData;
        PublishedSnapshot = publishedSnapshot;
        CurrentVersion = currentVersion;
        ConcurrencyToken = concurrencyToken;
        CreatedBy = createdBy;
        UpdatedBy = updatedBy;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        PublishedAt = publishedAt;
        PublishedBy = publishedBy;
        IsDeleted = isDeleted;
    }

    public Common.ContentEntryId Id { get; }

    public Common.ContentTypeId ContentTypeId { get; }

    public Common.Slug Slug { get; private set; }

    public ContentStatus Status { get; private set; }

    public ContentData DraftData { get; private set; }

    public ContentSnapshot? PublishedSnapshot { get; private set; }

    public VersionNumber CurrentVersion { get; private set; }

    public Common.ConcurrencyToken ConcurrencyToken { get; private set; }

    public Common.UserId CreatedBy { get; }

    public Common.UserId UpdatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Common.UserId? PublishedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public IReadOnlyList<ContentVersion> Versions => _versions.AsReadOnly();

    public bool HasPublishedRepresentation => PublishedSnapshot is not null && !IsDeleted;

    public static ContentEntry Create(
        Common.ContentTypeId contentTypeId,
        Common.Slug slug,
        Common.UserId createdBy,
        ContentData? initialData = null,
        DateTimeOffset? createdAt = null)
    {
        var timestamp = createdAt ?? DateTimeOffset.UtcNow;
        var data = initialData ?? ContentData.Empty;

        return new ContentEntry(
            Common.ContentEntryId.New(),
            contentTypeId,
            slug,
            ContentStatus.Draft,
            data,
            publishedSnapshot: null,
            VersionNumber.Initial,
            Common.ConcurrencyToken.Initial,
            createdBy,
            createdBy,
            timestamp,
            timestamp,
            publishedAt: null,
            publishedBy: null,
            isDeleted: false);
    }

    public static ContentEntry Restore(
        Common.ContentEntryId id,
        Common.ContentTypeId contentTypeId,
        Common.Slug slug,
        ContentStatus status,
        ContentData draftData,
        ContentSnapshot? publishedSnapshot,
        VersionNumber currentVersion,
        Common.ConcurrencyToken concurrencyToken,
        Common.UserId createdBy,
        Common.UserId updatedBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? publishedAt,
        Common.UserId? publishedBy,
        bool isDeleted,
        IEnumerable<ContentVersion> versions)
    {
        var entry = new ContentEntry(
            id,
            contentTypeId,
            slug,
            status,
            draftData,
            publishedSnapshot,
            currentVersion,
            concurrencyToken,
            createdBy,
            updatedBy,
            createdAt,
            updatedAt,
            publishedAt,
            publishedBy,
            isDeleted);

        entry._versions.AddRange(versions.OrderBy(version => version.VersionNumber.Value));
        entry.EnsureVersionIntegrity();
        entry.EnsurePublishedInvariants();
        return entry;
    }

    public ContentVersion UpdateDraft(
        ContentTypes.ContentType contentType,
        ContentData draftData,
        Common.Slug slug,
        Common.UserId updatedBy,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        if (contentType.Id != ContentTypeId)
        {
            throw new Common.DomainValidationException(nameof(contentType), "Content type does not match this entry.");
        }

        ContentDataValidator.Validate(contentType, draftData);

        DraftData = draftData;
        Slug = slug;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;

        if (Status == ContentStatus.Published)
        {
            ContentLifecycle.EnsureTransition(Status, ContentStatus.Draft);
            Status = ContentStatus.Draft;
        }

        ConcurrencyToken = ConcurrencyToken.Next();
        return RecordVersion(updatedBy, changeSummary, updatedAt);
    }

    public void SubmitForReview(Common.UserId actorId, Common.ConcurrencyToken expectedConcurrency, DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ContentLifecycle.EnsureTransition(Status, ContentStatus.InReview);

        Status = ContentStatus.InReview;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();
    }

    public void WithdrawFromReview(
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ContentLifecycle.EnsureTransition(Status, ContentStatus.Draft);

        Status = ContentStatus.Draft;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();
    }

    public ContentVersion Publish(
        ContentTypes.ContentType contentType,
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        if (contentType.Id != ContentTypeId)
        {
            throw new Common.DomainValidationException(nameof(contentType), "Content type does not match this entry.");
        }

        ContentLifecycle.EnsureTransition(Status, ContentStatus.Published);
        ContentDataValidator.Validate(contentType, DraftData);

        Status = ContentStatus.Published;
        PublishedSnapshot = new ContentSnapshot(Slug, DraftData.Clone(), ContentStatus.Published);
        PublishedAt = timestamp;
        PublishedBy = actorId;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();

        return RecordVersion(actorId, changeSummary, timestamp);
    }

    public ContentVersion Unpublish(
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        ContentLifecycle.EnsureTransition(Status, ContentStatus.Unpublished);

        Status = ContentStatus.Unpublished;
        PublishedSnapshot = null;
        PublishedAt = null;
        PublishedBy = null;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();

        var version = RecordVersion(actorId, changeSummary, timestamp);
        ContentLifecycle.EnsureTransition(Status, ContentStatus.Draft);
        Status = ContentStatus.Draft;
        return version;
    }

    public ContentVersion Archive(
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        ContentLifecycle.EnsureTransition(Status, ContentStatus.Archived);

        Status = ContentStatus.Archived;
        PublishedSnapshot = null;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();

        return RecordVersion(actorId, changeSummary, timestamp);
    }

    public ContentVersion RestoreFromArchive(
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        ContentLifecycle.EnsureTransition(Status, ContentStatus.Draft);

        Status = ContentStatus.Draft;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();

        return RecordVersion(actorId, changeSummary, timestamp);
    }

    public ContentVersion RestoreVersion(
        ContentVersion sourceVersion,
        Common.UserId actorId,
        Common.ConcurrencyToken expectedConcurrency,
        string changeSummary,
        DateTimeOffset timestamp)
    {
        EnsureNotDeleted();
        EnsureConcurrency(expectedConcurrency);
        ArgumentNullException.ThrowIfNull(sourceVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        if (sourceVersion.ContentEntryId != Id)
        {
            throw new Common.DomainValidationException(nameof(sourceVersion), "Version does not belong to this content entry.");
        }

        if (_versions.All(version => version.Id != sourceVersion.Id))
        {
            throw new Common.DomainValidationException(
                nameof(sourceVersion),
                "Version must belong to the entry version history.");
        }

        DraftData = sourceVersion.Snapshot.Data.Clone();
        Slug = sourceVersion.Snapshot.Slug;
        Status = ContentStatus.Draft;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();

        return RecordVersion(actorId, $"Restored from version {sourceVersion.VersionNumber.Value}: {changeSummary}", timestamp);
    }

    public void SoftDelete(Common.UserId actorId, DateTimeOffset timestamp)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();
    }

    public void RestoreDeleted(Common.UserId actorId, DateTimeOffset timestamp)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        UpdatedBy = actorId;
        UpdatedAt = timestamp;
        ConcurrencyToken = ConcurrencyToken.Next();
    }

    public ContentVersion? GetVersion(VersionNumber versionNumber) =>
        _versions.SingleOrDefault(version => version.VersionNumber == versionNumber);

    private ContentVersion RecordVersion(Common.UserId actorId, string changeSummary, DateTimeOffset timestamp)
    {
        if (_versions.Count > 0)
        {
            CurrentVersion = CurrentVersion.Next();
        }

        var snapshot = ContentSnapshot.FromEntry(this);
        var version = ContentVersion.Create(Id, CurrentVersion, snapshot, actorId, changeSummary, timestamp);
        _versions.Add(version);
        EnsurePublishedInvariants();
        return version;
    }

    private void EnsureConcurrency(Common.ConcurrencyToken expectedConcurrency)
    {
        if (ConcurrencyToken != expectedConcurrency)
        {
            throw new Common.ConcurrencyConflictException(expectedConcurrency.Value, ConcurrencyToken.Value);
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new Common.InvalidOperationDomainException("Deleted content entries cannot be modified.");
        }
    }

    private void EnsureVersionIntegrity()
    {
        if (_versions.Count == 0)
        {
            return;
        }

        var ordered = _versions.OrderBy(version => version.VersionNumber.Value).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var expected = index + 1;
            if (ordered[index].VersionNumber.Value != expected)
            {
                throw new Common.DomainValidationException(
                    nameof(_versions),
                    "Content versions must have monotonically increasing version numbers starting at 1.");
            }
        }

        if (ordered[^1].VersionNumber != CurrentVersion)
        {
            throw new Common.DomainValidationException(
                nameof(CurrentVersion),
                "Current version must match the latest recorded content version.");
        }
    }

    private void EnsurePublishedInvariants()
    {
        if (Status == ContentStatus.Published && PublishedSnapshot is null)
        {
            throw new Common.InvalidOperationDomainException("Published content entries must have a published snapshot.");
        }

        if (PublishedSnapshot is not null && Status == ContentStatus.Published && PublishedAt is null)
        {
            throw new Common.InvalidOperationDomainException("Published content entries must record publication metadata.");
        }
    }
}
