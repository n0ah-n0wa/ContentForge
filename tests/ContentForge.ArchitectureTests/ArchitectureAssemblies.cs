using System.Reflection;
using ApiProgram = ContentForge.Api.Program;

namespace ContentForge.ArchitectureTests;

internal static class ArchitectureAssemblies
{
    internal static Assembly Domain { get; } = typeof(ContentForge.Domain.DomainAssembly).Assembly;

    internal static Assembly Application { get; } = typeof(ContentForge.Application.ApplicationAssembly).Assembly;

    internal static Assembly Infrastructure { get; } = typeof(ContentForge.Infrastructure.DependencyInjection).Assembly;

    internal static Assembly Api { get; } = typeof(ApiProgram).Assembly;

    internal static IEnumerable<string> GetContentForgeProjectReferences(string projectName) =>
        ProjectReferenceReader.GetContentForgeProjectReferences(projectName);
}

internal static class ArchitectureRules
{
    internal static readonly string[] DomainForbiddenDependencies =
    [
        "ContentForge.Api",
        "ContentForge.Application",
        "ContentForge.Infrastructure",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "Azure",
        "Swashbuckle",
        "System.Net.Http",
    ];

    internal static readonly string[] ApplicationForbiddenDependencies =
    [
        "ContentForge.Api",
        "ContentForge.Infrastructure",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "Azure",
        "Swashbuckle",
    ];

    internal static readonly string[] InfrastructureForbiddenDependencies =
    [
        "ContentForge.Api",
    ];

    internal static readonly string[] ApiForbiddenDependencies =
    [
        "ContentForge.Domain",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Azure",
    ];

    internal static readonly string[] ApiAllowedContentForgeReferences =
    [
        "ContentForge.Application",
        "ContentForge.Infrastructure",
    ];

    internal static readonly string[] ApplicationAllowedContentForgeReferences =
    [
        "ContentForge.Domain",
    ];

    internal static readonly string[] InfrastructureAllowedContentForgeReferences =
    [
        "ContentForge.Application",
        "ContentForge.Domain",
    ];
}

internal static class TestResultExtensions
{
    internal static string GetFailureReport(this NetArchTest.Rules.TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        return string.Join(", ", result.FailingTypes.Select(type => type.FullName));
    }
}
