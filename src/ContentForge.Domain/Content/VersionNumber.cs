namespace ContentForge.Domain.Content;

/// <summary>
/// Monotonically increasing version number for a content entry.
/// </summary>
public readonly record struct VersionNumber(int Value)
{
    public static VersionNumber Initial { get; } = new(1);

    public VersionNumber Next() => new(Value + 1);

    public static VersionNumber From(int value)
    {
        if (value <= 0)
        {
            throw new Common.DomainValidationException(nameof(value), "Version numbers must be greater than zero.");
        }

        return new VersionNumber(value);
    }
}
