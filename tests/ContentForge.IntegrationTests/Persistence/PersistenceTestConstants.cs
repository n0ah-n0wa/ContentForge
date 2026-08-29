namespace ContentForge.IntegrationTests.Persistence;

using ContentForge.Domain.Common;

internal static class PersistenceTestConstants
{
    internal const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=contentforge_test;Username=contentforge;Password=contentforge";

    internal static readonly UserId ActorId = UserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    internal static readonly DateTimeOffset BaseTimestamp =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
}
