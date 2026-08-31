namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection(PersistenceTests.Name)]
public abstract class PersistenceTestBase : IAsyncLifetime
{
    protected PersistenceTestBase(PostgreSqlPersistenceFixture fixture)
    {
        Fixture = fixture;
    }

    protected PostgreSqlPersistenceFixture Fixture { get; }

    protected PersistenceScope CreatePersistenceScope() => new(Fixture.CreateScope());

    public async Task InitializeAsync() => await Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected sealed class PersistenceScope : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;

        internal PersistenceScope(AsyncServiceScope scope)
        {
            _scope = scope;
            DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ContentTypes = scope.ServiceProvider.GetRequiredService<IContentTypeRepository>();
            ContentEntries = scope.ServiceProvider.GetRequiredService<IContentEntryRepository>();
            MediaAssets = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            FileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            AuditLogs = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            UnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            AuditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            ContentSearch = scope.ServiceProvider.GetRequiredService<IContentSearchService>();
        }

        internal AppDbContext DbContext { get; }

        internal IContentTypeRepository ContentTypes { get; }

        internal IContentEntryRepository ContentEntries { get; }

        internal IMediaRepository MediaAssets { get; }

        internal IFileStorage FileStorage { get; }

        internal IAuditLogRepository AuditLogs { get; }

        internal IUnitOfWork UnitOfWork { get; }

        internal IAuditService AuditService { get; }

        internal IContentSearchService ContentSearch { get; }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
