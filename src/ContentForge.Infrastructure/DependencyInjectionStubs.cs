namespace ContentForge.Infrastructure;

using ContentForge.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

internal static class DependencyInjectionStubs
{
    internal static IServiceCollection AddApplicationPortStubs(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IFileStorage, NotImplementedFileStorage>();

        return services;
    }

    private sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
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
