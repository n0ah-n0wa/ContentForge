using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Auth.Models;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ContentForge.Infrastructure;

internal static class DependencyInjectionStubs
{
    internal static IServiceCollection AddApplicationPortStubs(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, UnauthenticatedCurrentUserService>();
        services.AddScoped<IPasswordHasher, NotImplementedPasswordHasher>();
        services.AddScoped<IAuthenticationService, NotImplementedAuthenticationService>();
        services.AddScoped<IFileStorage, NotImplementedFileStorage>();

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
}
