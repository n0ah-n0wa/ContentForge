using System.Xml.Linq;

namespace ContentForge.ArchitectureTests;

internal static class ProjectReferenceReader
{
    private static readonly Lazy<string> _solutionRoot = new(LocateSolutionRoot);

    internal static IEnumerable<string> GetContentForgeProjectReferences(string projectName)
    {
        var projectPath = Path.Combine(_solutionRoot.Value, "src", projectName, $"{projectName}.csproj");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}", projectPath);
        }

        var document = XDocument.Load(projectPath);
        var projectReferenceElement = document.Root?.Name.LocalName == "Project"
            ? XName.Get("ProjectReference", document.Root.Name.NamespaceName)
            : "ProjectReference";

        return document
            .Descendants(projectReferenceElement)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => include is not null)
            .Select(include => GetReferencedProjectName(include!))
            .Where(name => name.StartsWith("ContentForge.", StringComparison.Ordinal));
    }

    private static string GetReferencedProjectName(string includePath)
    {
        var normalizedPath = includePath.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFileNameWithoutExtension(normalizedPath);
    }

    private static string LocateSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentForge.sln")) ||
                File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ContentForge solution root.");
    }
}
