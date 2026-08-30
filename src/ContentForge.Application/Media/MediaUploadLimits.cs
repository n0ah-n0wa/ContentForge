namespace ContentForge.Application.Media;

using ContentForge.Domain.Media;

/// <summary>
/// Application-facing media upload limits aligned with domain constraints.
/// </summary>
public static class MediaUploadLimits
{
    public const long MaxFileSizeBytes = MediaUploadRules.MaxFileSizeBytes;
}
