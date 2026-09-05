namespace ContentForge.Infrastructure;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContentForge.Application.Abstractions;
using ContentForge.Infrastructure.Authorization;
using ContentForge.Infrastructure.Identity;
using ContentForge.Infrastructure.Options;
using ContentForge.Infrastructure.Persistence;
using ContentForge.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

internal static class DependencyInjectionAuthentication
{
    private static readonly string[] _forbiddenSigningKeyMarkers =
    [
        "DEV_ONLY",
        "TEST_ONLY",
        "CHANGE_ME",
        "REPLACE_ME",
    ];

    internal static IServiceCollection AddContentForgeAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        services.AddIdentityCore<ContentForgeUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IPasswordResetService, IdentityPasswordResetService>();
        services.AddScoped<ISessionInvalidationService, IdentitySessionInvalidationService>();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<RefreshTokenService>();

        RegisterPasswordResetDelivery(services, configuration, environment);

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing.");

        var isNonProduction = IsNonProduction(configuration, environment);
        ValidateJwtOptions(jwtOptions, isNonProduction);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !isNonProduction;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateActiveUserAsync,
                };
            });

        services.AddContentForgeAuthorization();
        return services;
    }

    private static async Task ValidateActiveUserAsync(TokenValidatedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ContentForgeUser>>();
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            context.Fail("Invalid subject claim.");
            return;
        }

        var user = await userManager.FindByIdAsync(parsedUserId.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            context.Fail("User is disabled or missing.");
            return;
        }

        var stampClaim = context.Principal?.FindFirstValue(JwtTokenService.SecurityStampClaimType);
        if (string.IsNullOrWhiteSpace(stampClaim)
            || !string.Equals(stampClaim, user.SecurityStamp, StringComparison.Ordinal))
        {
            context.Fail("Security stamp mismatch.");
        }
    }

    private static void ValidateJwtOptions(JwtOptions jwtOptions, bool isNonProduction)
    {
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            throw new InvalidOperationException("JWT issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException("JWT audience must be configured.");
        }

        if (jwtOptions.AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("JWT access token lifetime must be greater than zero minutes.");
        }

        if (jwtOptions.RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException("JWT refresh token lifetime must be greater than zero days.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 characters.");
        }

        if (isNonProduction)
        {
            return;
        }

        if (_forbiddenSigningKeyMarkers.Any(marker =>
                jwtOptions.SigningKey.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "JWT signing key appears to be a development placeholder. Configure a production secret.");
        }
    }

    private static void RegisterPasswordResetDelivery(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));
        var options = configuration.GetSection(PasswordResetOptions.SectionName).Get<PasswordResetOptions>()
            ?? new PasswordResetOptions();
        var mode = options.DeliveryMode?.Trim() ?? "Logging";

        if (string.Equals(mode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Host)
                || string.IsNullOrWhiteSpace(options.Smtp.FromAddress)
                || string.IsNullOrWhiteSpace(options.PublicAppBaseUrl))
            {
                throw new InvalidOperationException(
                    "PasswordReset DeliveryMode=Smtp requires Smtp:Host, Smtp:FromAddress, and PublicAppBaseUrl.");
            }

            services.AddSingleton<IPasswordResetNotifier, SmtpPasswordResetNotifier>();
            return;
        }

        if (string.Equals(mode, "Logging", StringComparison.OrdinalIgnoreCase))
        {
            // Only Environments.Production forbids Logging. Staging may log tokens for ops
            // until SMTP is configured; IsNonProduction is intentionally narrower (Dev/Testing)
            // so Staging still enforces production-grade JWT rules.
            if (IsProductionEnvironment(configuration, environment))
            {
                throw new InvalidOperationException(
                    "PasswordReset DeliveryMode=Logging is not allowed in Production. Configure DeliveryMode=Smtp.");
            }

            // Capturing notifier is used by integration tests; Development also logs via decorator below.
            services.AddSingleton<CapturingPasswordResetNotifier>();
            services.AddSingleton<IPasswordResetNotifier>(sp =>
            {
                var capture = sp.GetRequiredService<CapturingPasswordResetNotifier>();
                if (environment?.IsEnvironment("Testing") == true)
                {
                    return capture;
                }

                return new CompositePasswordResetNotifier(
                    capture,
                    ActivatorUtilities.CreateInstance<LoggingPasswordResetNotifier>(sp));
            });
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported PasswordReset:DeliveryMode '{mode}'. Use 'Logging' or 'Smtp'.");
    }

    private static bool IsNonProduction(IConfiguration configuration, IHostEnvironment? environment)
    {
        if (environment is not null)
        {
            return environment.IsDevelopment()
                || environment.IsEnvironment("Testing");
        }

        var environmentName = ResolveEnvironmentName(configuration);

        return environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase)
            || environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductionEnvironment(IConfiguration configuration, IHostEnvironment? environment)
    {
        if (environment is not null)
        {
            return environment.IsProduction();
        }

        return ResolveEnvironmentName(configuration)
            .Equals(Environments.Production, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveEnvironmentName(IConfiguration configuration) =>
        configuration[HostDefaults.EnvironmentKey]
        ?? configuration["ASPNETCORE_ENVIRONMENT"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environments.Production;
}
