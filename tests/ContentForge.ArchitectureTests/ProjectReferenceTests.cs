using FluentAssertions;

namespace ContentForge.ArchitectureTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void Domain_ShouldNotReferenceOtherContentForgeProjects()
    {
        ArchitectureAssemblies.GetContentForgeProjectReferences("ContentForge.Domain")
            .Should()
            .BeEmpty("Domain must not reference Api, Application, or Infrastructure.");
    }

    [Fact]
    public void Application_ShouldOnlyReferenceDomain()
    {
        ArchitectureAssemblies.GetContentForgeProjectReferences("ContentForge.Application")
            .Should()
            .BeEquivalentTo(ArchitectureRules.ApplicationAllowedContentForgeReferences);
    }

    [Fact]
    public void Infrastructure_ShouldOnlyReferenceApplicationAndDomain()
    {
        ArchitectureAssemblies.GetContentForgeProjectReferences("ContentForge.Infrastructure")
            .Should()
            .BeEquivalentTo(ArchitectureRules.InfrastructureAllowedContentForgeReferences);
    }

    [Fact]
    public void Api_ShouldOnlyReferenceApplicationAndInfrastructure()
    {
        ArchitectureAssemblies.GetContentForgeProjectReferences("ContentForge.Api")
            .Should()
            .BeEquivalentTo(ArchitectureRules.ApiAllowedContentForgeReferences);
    }

    [Fact]
    public void Api_ShouldNotDirectlyReferenceDomain()
    {
        ArchitectureAssemblies.GetContentForgeProjectReferences("ContentForge.Api")
            .Should()
            .NotContain("ContentForge.Domain", "Api must depend on Domain only transitively through Application.");
    }
}
