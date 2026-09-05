namespace ContentForge.UnitTests.Docker;

using FluentAssertions;

public sealed class NginxForwardedHeadersHardeningTests
{
    [Fact]
    public void NginxTemplate_OverwritesClientSuppliedXForwardedFor()
    {
        var path = FindRepoFile(Path.Combine("infra", "docker", "web", "nginx.conf.template"));
        var contents = File.ReadAllText(path);

        contents.Should().Contain("proxy_set_header X-Forwarded-For $remote_addr;");
        contents.Should().NotContain("proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
        contents.Should().Contain("location /media-files/");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' from test base directory.");
    }
}
