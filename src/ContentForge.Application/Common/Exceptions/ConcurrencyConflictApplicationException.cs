namespace ContentForge.Application.Common.Exceptions;

using ContentForge.Application.Common.Concurrency;

/// <summary>
/// Thrown when an optimistic concurrency check fails.
/// </summary>
public sealed class ConcurrencyConflictApplicationException : ApplicationException
{
    public ConcurrencyConflictApplicationException(ConcurrencyConflictResult conflict)
        : base("The resource was modified by another user.")
    {
        Conflict = conflict;
    }

    public ConcurrencyConflictResult Conflict { get; }
}
