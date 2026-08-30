namespace ContentForge.Api.Infrastructure;

/// <summary>
/// Shared API routing and documentation constants.
/// </summary>
internal static class ApiConstants
{
    internal const string VersionPrefix = "api/v1";
    internal const string Version = "1.0";
    internal const string ApiTitle = "ContentForge API";

    internal static class ErrorTypes
    {
        internal const string Validation = "https://contentforge/errors/validation";
        internal const string Unauthorized = "https://contentforge/errors/unauthorized";
        internal const string Forbidden = "https://contentforge/errors/forbidden";
        internal const string NotFound = "https://contentforge/errors/not-found";
        internal const string Conflict = "https://contentforge/errors/conflict";
        internal const string BadRequest = "https://contentforge/errors/bad-request";
        internal const string Application = "https://contentforge/errors/application";
    }
}
