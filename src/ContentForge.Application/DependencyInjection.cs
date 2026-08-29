using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Auth.Commands;
using ContentForge.Application.Auth.Queries;
using ContentForge.Application.Content.Commands;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.ContentTypes.Commands;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Application.Media.Commands;
using ContentForge.Application.Media.Queries;
using ContentForge.Application.PublicContent.Queries;
using ContentForge.Application.Users.Commands;
using ContentForge.Application.Users.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContentForge.Application;

/// <summary>
/// Dependency injection registration for application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        RegisterHandlers(services);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<GetAuthenticatedUserQueryHandler>();

        services.AddScoped<CreateContentTypeCommandHandler>();
        services.AddScoped<UpdateContentTypeCommandHandler>();
        services.AddScoped<DeleteContentTypeCommandHandler>();
        services.AddScoped<AddContentTypeFieldCommandHandler>();
        services.AddScoped<GetContentTypeQueryHandler>();
        services.AddScoped<ListContentTypesQueryHandler>();

        services.AddScoped<CreateContentEntryCommandHandler>();
        services.AddScoped<UpdateContentEntryCommandHandler>();
        services.AddScoped<DeleteContentEntryCommandHandler>();
        services.AddScoped<SubmitContentForReviewCommandHandler>();
        services.AddScoped<WithdrawContentFromReviewCommandHandler>();
        services.AddScoped<PublishContentCommandHandler>();
        services.AddScoped<UnpublishContentCommandHandler>();
        services.AddScoped<ArchiveContentCommandHandler>();
        services.AddScoped<RestoreArchivedContentCommandHandler>();
        services.AddScoped<RestoreContentVersionCommandHandler>();
        services.AddScoped<GetContentEntryQueryHandler>();
        services.AddScoped<ListContentEntriesQueryHandler>();
        services.AddScoped<ListContentVersionsQueryHandler>();
        services.AddScoped<GetContentVersionQueryHandler>();
        services.AddScoped<CompareContentVersionsQueryHandler>();
        services.AddScoped<SearchContentQueryHandler>();

        services.AddScoped<UploadMediaCommandHandler>();
        services.AddScoped<UpdateMediaMetadataCommandHandler>();
        services.AddScoped<DeleteMediaCommandHandler>();
        services.AddScoped<GetMediaQueryHandler>();
        services.AddScoped<ListMediaQueryHandler>();

        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<UpdateUserCommandHandler>();
        services.AddScoped<DisableUserCommandHandler>();
        services.AddScoped<GetUserQueryHandler>();
        services.AddScoped<ListUsersQueryHandler>();
        services.AddScoped<ListRolesQueryHandler>();

        services.AddScoped<GetAuditLogQueryHandler>();
        services.AddScoped<ListAuditLogsQueryHandler>();

        services.AddScoped<ListPublicContentQueryHandler>();
        services.AddScoped<GetPublicContentBySlugQueryHandler>();
    }
}
