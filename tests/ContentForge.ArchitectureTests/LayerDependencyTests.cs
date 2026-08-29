using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using ApiProgram = ContentForge.Api.Program;

namespace ContentForge.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly _domainAssembly = typeof(ContentForge.Domain.DomainAssembly).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(ContentForge.Application.ApplicationAssembly).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(ContentForge.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly _apiAssembly = typeof(ApiProgram).Assembly;

    [Fact]
    public void Domain_ShouldNotReferenceInfrastructure()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Domain_ShouldNotReferenceApi()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Domain_ShouldNotReferenceApplication()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Application_ShouldNotReferenceInfrastructure()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Application_ShouldNotReferenceApi()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        var result = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("ContentForge.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Fact]
    public void Api_ShouldReferenceApplicationAndInfrastructure()
    {
        _apiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should()
            .Contain(["ContentForge.Application", "ContentForge.Infrastructure"]);
    }
}

internal static class TestResultExtensions
{
    internal static string GetFailureReport(this TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
