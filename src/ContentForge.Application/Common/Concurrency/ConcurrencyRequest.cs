namespace ContentForge.Application.Common.Concurrency;

/// <summary>
/// Optimistic concurrency token supplied by clients for mutating operations.
/// </summary>
public sealed record ConcurrencyRequest(uint Version);
