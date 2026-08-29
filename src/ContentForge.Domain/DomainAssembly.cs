namespace ContentForge.Domain;

/// <summary>
/// Assembly marker for the ContentForge domain layer.
/// </summary>
public static class DomainAssembly
{
    /// <summary>
    /// Gets the domain assembly name.
    /// </summary>
    public static string Name => typeof(DomainAssembly).Assembly.GetName().Name ?? "ContentForge.Domain";
}
