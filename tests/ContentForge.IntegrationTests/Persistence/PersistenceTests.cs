namespace ContentForge.IntegrationTests.Persistence;

[CollectionDefinition(PersistenceTests.Name)]
public sealed class PersistenceTests : ICollectionFixture<PostgreSqlPersistenceFixture>
{
    public const string Name = "Persistence";
}
