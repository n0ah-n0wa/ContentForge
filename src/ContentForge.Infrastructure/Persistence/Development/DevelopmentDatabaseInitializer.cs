namespace ContentForge.Infrastructure.Persistence.Development;

using ContentForge.Domain.Authorization;
using ContentForge.Infrastructure.Persistence.Entities;
using ContentForge.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Applies EF Core migrations and seeds a default administrator in Development.
/// </summary>
internal sealed class DevelopmentDatabaseInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<DevelopmentDatabaseInitializer> logger) : IHostedService
{
    internal const string DefaultAdminEmail = "admin@contentforge.local";
    internal const string DefaultAdminPassword = "AdminPassword123!";
    internal static readonly Guid DefaultAdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ContentForgeUser>>();

        DevelopmentDatabaseInitializerLogger.ApplyingMigrations(logger);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        if (await userManager.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            DevelopmentDatabaseInitializerLogger.SkippingSeed(logger);
            return;
        }

        var admin = new ContentForgeUser
        {
            Id = DefaultAdminUserId,
            UserName = DefaultAdminEmail,
            NormalizedUserName = DefaultAdminEmail.ToUpperInvariant(),
            Email = DefaultAdminEmail,
            NormalizedEmail = DefaultAdminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = "Administrator",
            IsActive = true,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UserRoles =
            [
                new UserRoleEntity
                {
                    UserId = DefaultAdminUserId,
                    RoleId = AuthorizationSeedIds.AdministratorRoleId,
                },
            ],
        };

        var result = await userManager.CreateAsync(admin, DefaultAdminPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed Development administrator: {errors}");
        }

        DevelopmentDatabaseInitializerLogger.SeededAdministrator(logger, DefaultAdminEmail);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
