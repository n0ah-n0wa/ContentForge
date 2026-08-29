using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace ContentForge.ArchitectureTests;

public sealed class ForbiddenDependencyTests
{
    private static readonly Assembly _domainAssembly = typeof(ContentForge.Domain.DomainAssembly).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(ContentForge.Application.ApplicationAssembly).Assembly;

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Npgsql")]
    [InlineData("Azure")]
    [InlineData("Swashbuckle")]
    public void Domain_ShouldNotDependOnForbiddenNamespaces(string forbiddenNamespace)
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Npgsql")]
    [InlineData("Azure")]
    public void Application_ShouldNotDependOnForbiddenNamespaces(string forbiddenNamespace)
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.GetFailureReport());
    }
}
