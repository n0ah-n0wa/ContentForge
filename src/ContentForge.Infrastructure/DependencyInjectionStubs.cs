namespace ContentForge.Infrastructure;

using ContentForge.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class DependencyInjectionStubs
{
    internal static IServiceCollection AddApplicationPortStubs(
        this IServiceCollection services,
        IHostEnvironment? environment = null)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        if (environment?.IsEnvironment("Testing") == true || environment?.IsDevelopment() == true)
        {
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();
        }
        else
        {
            services.AddScoped<IFileStorage, NotImplementedFileStorage>();
        }

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
