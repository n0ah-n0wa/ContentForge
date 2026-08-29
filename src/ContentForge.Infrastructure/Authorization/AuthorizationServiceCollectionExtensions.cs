namespace ContentForge.Infrastructure.Authorization;

using ContentForge.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

internal static class AuthorizationServiceCollectionExtensions
{
    internal static IServiceCollection AddContentForgeAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                var policyName = permission.Value;
                options.AddPolicy(
                    policyName,
                    policy => policy.RequireAuthenticatedUser()
                        .AddRequirements(new PermissionRequirement(policyName)));
            }
        });

        return services;
    }
}
