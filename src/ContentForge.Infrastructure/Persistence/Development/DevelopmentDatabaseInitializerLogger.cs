namespace ContentForge.Infrastructure.Persistence.Development;

using Microsoft.Extensions.Logging;

internal static partial class DevelopmentDatabaseInitializerLogger
{
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Applying database migrations for Development.")]
    public static partial void ApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Information,
        Message = "Development database already contains users; skipping seed.")]
    public static partial void SkippingSeed(ILogger logger);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Information,
        Message = "Seeded Development administrator account {Email}. Change the password after first login.")]
    public static partial void SeededAdministrator(ILogger logger, string email);
}
