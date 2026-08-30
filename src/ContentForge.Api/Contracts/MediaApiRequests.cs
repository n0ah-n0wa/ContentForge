namespace ContentForge.Api.Contracts;

/// <summary>
/// Request body for updating media metadata.
/// </summary>
public sealed record UpdateMediaMetadataApiRequest(
    string? AltText,
    string? Title,
    string? Description);
