namespace ContentForge.Domain.Media;

using ContentForge.Domain.Common;

/// <summary>
/// Represents media metadata stored outside the relational database.
/// </summary>
public sealed class MediaAsset
{
    private MediaAsset(
        MediaId id,
        string fileName,
        string originalFileName,
        string contentType,
        long size,
        StorageKey storageKey,
        string? url,
        int? width,
        int? height,
        string? altText,
        string? title,
        string? description,
        UserId uploadedBy,
        DateTimeOffset uploadedAt,
        bool isDeleted)
    {
        Id = id;
        FileName = fileName;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        Size = size;
        StorageKey = storageKey;
        Url = url;
        Width = width;
        Height = height;
        AltText = altText;
        Title = title;
        Description = description;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
        IsDeleted = isDeleted;
    }

    public MediaId Id { get; }

    public string FileName { get; private set; }

    public string OriginalFileName { get; }

    public string ContentType { get; private set; }

    public long Size { get; }

    public StorageKey StorageKey { get; }

    public string? Url { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public string? AltText { get; private set; }

    public string? Title { get; private set; }

    public string? Description { get; private set; }

    public UserId UploadedBy { get; }

    public DateTimeOffset UploadedAt { get; }

    public bool IsDeleted { get; private set; }

    public static MediaAsset Create(
        string fileName,
        string originalFileName,
        string contentType,
        long size,
        StorageKey storageKey,
        UserId uploadedBy,
        string? url = null,
        int? width = null,
        int? height = null,
        string? altText = null,
        string? title = null,
        string? description = null,
        DateTimeOffset? uploadedAt = null) =>
        Create(
            MediaId.New(),
            fileName,
            originalFileName,
            contentType,
            size,
            storageKey,
            uploadedBy,
            url,
            width,
            height,
            altText,
            title,
            description,
            uploadedAt);

    public static MediaAsset Create(
        MediaId id,
        string fileName,
        string originalFileName,
        string contentType,
        long size,
        StorageKey storageKey,
        UserId uploadedBy,
        string? url = null,
        int? width = null,
        int? height = null,
        string? altText = null,
        string? title = null,
        string? description = null,
        DateTimeOffset? uploadedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var isolatedOriginal = MediaUploadRules.IsolateFileName(originalFileName);
        var isolatedStoredName = MediaUploadRules.IsolateFileName(fileName);
        MediaUploadRules.EnsureAllowed(isolatedOriginal, contentType, size);

        return new MediaAsset(
            id,
            isolatedStoredName,
            isolatedOriginal,
            MediaUploadRules.NormalizeContentType(contentType),
            size,
            storageKey,
            url,
            width,
            height,
            altText,
            title,
            description,
            uploadedBy,
            uploadedAt ?? DateTimeOffset.UtcNow,
            isDeleted: false);
    }

    internal static MediaAsset Restore(
        MediaId id,
        string fileName,
        string originalFileName,
        string contentType,
        long size,
        StorageKey storageKey,
        UserId uploadedBy,
        string? url,
        int? width,
        int? height,
        string? altText,
        string? title,
        string? description,
        DateTimeOffset uploadedAt,
        bool isDeleted) =>
        new(
            id,
            fileName,
            originalFileName,
            contentType,
            size,
            storageKey,
            url,
            width,
            height,
            altText,
            title,
            description,
            uploadedBy,
            uploadedAt,
            isDeleted);

    public void UpdateMetadata(
        string? altText,
        string? title,
        string? description,
        string? url = null)
    {
        EnsureNotDeleted();

        AltText = altText;
        Title = title;
        Description = description;

        if (url is not null)
        {
            Url = url;
        }
    }

    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
    }

    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationDomainException("Deleted media assets cannot be modified.");
        }
    }
}
