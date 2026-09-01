namespace ContentForge.ArchitectureTests;

internal static class MigrationRepositoryPaths
{
    internal static string Root
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "ContentForge.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root from test output directory.");
        }
    }
}
