using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Audit.Queries;
using ContentForge.Application.Auth.Models;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.ContentTypes.Queries;
using ContentForge.Application.Media.Queries;
using ContentForge.Application.Users.Models;
using ContentForge.Application.Users.Queries;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.Domain.Media;
using Microsoft.Extensions.DependencyInjection;

namespace ContentForge.Infrastructure;

internal static class DependencyInjectionStubs
{
    internal static IServiceCollection AddApplicationPortStubs(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, UnauthenticatedCurrentUserService>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        services.AddScoped<IAuditService, NoOpAuditService>();
        services.AddScoped<IPasswordHasher, NotImplementedPasswordHasher>();
        services.AddScoped<IAuthenticationService, NotImplementedAuthenticationService>();
        services.AddScoped<IFileStorage, NotImplementedFileStorage>();
        services.AddScoped<IContentSearchService, NotImplementedContentSearchService>();
        services.AddScoped<IContentTypeRepository, NotImplementedContentTypeRepository>();
        services.AddScoped<IContentEntryRepository, NotImplementedContentEntryRepository>();
        services.AddScoped<IMediaRepository, NotImplementedMediaRepository>();
        services.AddScoped<IAuditLogRepository, NotImplementedAuditLogRepository>();
        services.AddScoped<IUserRepository, NotImplementedUserRepository>();

        return services;
    }

    private sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class UnauthenticatedCurrentUserService : ICurrentUserService
    {
        public UserId? UserId => null;

        public RoleDefinition? Role => null;

        public bool IsAuthenticated => false;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task RecordAsync(
            AuditAction action,
            string entityType,
            string entityId,
            UserId? userId = null,
            string? metadata = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NotImplementedPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) =>
            throw new NotImplementedException();

        public bool VerifyPassword(string password, string passwordHash) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            LoginRequest request,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthenticationResult> RefreshTokenAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedFileStorage : IFileStorage
    {
        public Task<string> UploadAsync(Stream content, string contentType, string storageKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedContentSearchService : IContentSearchService
    {
        public Task<PaginatedResult<Guid>> SearchAsync(ContentSearchCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedContentTypeRepository : IContentTypeRepository
    {
        public Task<ContentType?> GetByIdAsync(ContentTypeId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ContentType?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExistsByNameAsync(FieldName name, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasDependentEntriesAsync(ContentTypeId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<ContentType>> ListAsync(ContentTypeListCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(ContentType contentType, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(ContentType contentType, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task DeleteAsync(ContentType contentType, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedContentEntryRepository : IContentEntryRepository
    {
        public Task<ContentEntry?> GetByIdAsync(ContentEntryId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ContentEntry?> GetBySlugAsync(ContentTypeId contentTypeId, Slug slug, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExistsBySlugAsync(ContentTypeId contentTypeId, Slug slug, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<ContentEntry>> ListAsync(ContentEntryListCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(ContentEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(ContentEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedMediaRepository : IMediaRepository
    {
        public Task<MediaAsset?> GetByIdAsync(MediaId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<MediaAsset>> ListAsync(MediaListCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(MediaAsset asset, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedAuditLogRepository : IAuditLogRepository
    {
        public Task<AuditLogEntry?> GetByIdAsync(AuditLogId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<AuditLogEntry>> ListAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class NotImplementedUserRepository : IUserRepository
    {
        public Task<UserAccount?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<UserAccount>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(UserAccount user, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task UpdateAsync(UserAccount user, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<RoleDefinition>> ListRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleDefinition>>(DefaultRoleDefinitions.All.Values.ToList());
    }
}
