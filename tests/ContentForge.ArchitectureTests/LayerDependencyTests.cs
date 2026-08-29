using FluentAssertions;
using NetArchTest.Rules;

namespace ContentForge.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Theory]
    [MemberData(nameof(DomainForbiddenDependencyCases))]
    public void Domain_ShouldNotHaveForbiddenDependency(string forbiddenDependency)
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOn(forbiddenDependency)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain must not depend on '{forbiddenDependency}'. Failing types: {result.GetFailureReport()}");
    }

    [Theory]
    [MemberData(nameof(ApplicationForbiddenDependencyCases))]
    public void Application_ShouldNotHaveForbiddenDependency(string forbiddenDependency)
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Application)
            .ShouldNot()
            .HaveDependencyOn(forbiddenDependency)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application must not depend on '{forbiddenDependency}'. Failing types: {result.GetFailureReport()}");
    }

    [Theory]
    [MemberData(nameof(InfrastructureForbiddenDependencyCases))]
    public void Infrastructure_ShouldNotHaveForbiddenDependency(string forbiddenDependency)
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(forbiddenDependency)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Infrastructure must not depend on '{forbiddenDependency}'. Failing types: {result.GetFailureReport()}");
    }

    [Theory]
    [MemberData(nameof(ApiForbiddenDependencyCases))]
    public void Api_ShouldNotHaveForbiddenDependency(string forbiddenDependency)
    {
        var result = Types.InAssembly(ArchitectureAssemblies.Api)
            .ShouldNot()
            .HaveDependencyOn(forbiddenDependency)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Api must not depend on '{forbiddenDependency}'. Failing types: {result.GetFailureReport()}");
    }

    public static IEnumerable<object[]> DomainForbiddenDependencyCases() =>
        ArchitectureRules.DomainForbiddenDependencies.Select(item => new object[] { item });

    public static IEnumerable<object[]> ApplicationForbiddenDependencyCases() =>
        ArchitectureRules.ApplicationForbiddenDependencies.Select(item => new object[] { item });

    public static IEnumerable<object[]> InfrastructureForbiddenDependencyCases() =>
        ArchitectureRules.InfrastructureForbiddenDependencies.Select(item => new object[] { item });

    public static IEnumerable<object[]> ApiForbiddenDependencyCases() =>
        ArchitectureRules.ApiForbiddenDependencies.Select(item => new object[] { item });
}
