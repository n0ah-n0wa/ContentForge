namespace ContentForge.Infrastructure.Options;

/// <summary>
/// Object-storage settings for media binaries.
/// </summary>
public sealed class MediaStorageOptions
{
    public const string SectionName = "Media";

    /// <summary>
    /// Storage backend: Local, InMemory, or Azure.
    /// </summary>
    public string Provider { get; set; } = "Local";

    public string LocalRoot { get; set; } = "App_Data/media";

    /// <summary>
    /// Public URL prefix returned in media metadata (CDN, App Service route, or blob endpoint prefix).
    /// </summary>
    public string PublicBaseUrl { get; set; } = "/media-files";

    /// <summary>
    /// Azure Blob Storage container name. Required when Provider is Azure.
    /// </summary>
    public string ContainerName { get; set; } = "media";

    /// <summary>
    /// Optional storage account connection string. Use for Azurite/local emulation only; prefer managed identity in production.
    /// Supply via environment variable or secret store — never commit credentials.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Full blob service URI, e.g. https://account.blob.core.windows.net or http://127.0.0.1:10000/devstoreaccount1 for Azurite.
    /// </summary>
    public string? BlobServiceUri { get; set; }

    /// <summary>
    /// Azure storage account name. Used with managed identity when BlobServiceUri is not set.
    /// </summary>
    public string? StorageAccountName { get; set; }

    /// <summary>
    /// When true and ConnectionString is empty, authenticate with <see cref="Azure.Identity.DefaultAzureCredential"/> (managed identity in Azure).
    /// </summary>
    public bool UseManagedIdentity { get; set; }
}
