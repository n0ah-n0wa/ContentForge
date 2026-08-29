namespace ContentForge.Application;

/// <summary>
/// Assembly marker for the ContentForge application layer.
/// </summary>
public static class ApplicationAssembly
{
    /// <summary>
    /// Gets the application assembly name.
    /// </summary>
    public static string Name => typeof(ApplicationAssembly).Assembly.GetName().Name ?? "ContentForge.Application";
}
