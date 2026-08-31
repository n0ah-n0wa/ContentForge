namespace ContentForge.Application.ContentPreview;

/// <summary>
/// Configuration for short-lived content preview tokens.
/// </summary>
public sealed class ContentPreviewOptions
{
    public const string SectionName = "ContentPreview";

    public int TokenLifetimeMinutes { get; init; } = 15;
}
