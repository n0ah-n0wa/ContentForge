namespace ContentForge.Domain.Common;

/// <summary>
/// Represents an optimistic concurrency token for aggregate roots.
/// </summary>
public readonly record struct ConcurrencyToken
{
    public ConcurrencyToken(uint value)
    {
        if (value == 0)
        {
            throw new DomainValidationException(nameof(value), "Concurrency token must be greater than zero.");
        }

        Value = value;
    }

    public uint Value { get; }

    public static ConcurrencyToken Initial { get; } = new(1);

    public ConcurrencyToken Next() => new(Value + 1);
}
