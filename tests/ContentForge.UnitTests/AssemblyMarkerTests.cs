using ContentForge.Application;
using ContentForge.Domain;
using FluentAssertions;

namespace ContentForge.UnitTests;

public sealed class AssemblyMarkerTests
{
    [Fact]
    public void DomainAssembly_HasExpectedName()
    {
        DomainAssembly.Name.Should().Be("ContentForge.Domain");
    }

    [Fact]
    public void ApplicationAssembly_HasExpectedName()
    {
        ApplicationAssembly.Name.Should().Be("ContentForge.Application");
    }
}
