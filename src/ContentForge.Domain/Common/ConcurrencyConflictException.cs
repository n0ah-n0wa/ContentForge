namespace ContentForge.Domain.Common;

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(uint expectedVersion, uint actualVersion)
        : base($"Concurrency conflict: expected row version {expectedVersion}, but current version is {actualVersion}.")
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public uint ExpectedVersion { get; }

    public uint ActualVersion { get; }
}
