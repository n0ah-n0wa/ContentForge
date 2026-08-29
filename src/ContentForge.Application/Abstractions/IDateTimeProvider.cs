namespace ContentForge.Application.Abstractions;

/// <summary>
/// Abstraction for retrieving the current UTC timestamp.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
