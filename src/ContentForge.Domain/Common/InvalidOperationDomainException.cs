namespace ContentForge.Domain.Common;

/// <summary>
/// Thrown when a requested domain operation is not allowed in the current state.
/// </summary>
public sealed class InvalidOperationDomainException : DomainException
{
    public InvalidOperationDomainException(string message)
        : base(message)
    {
    }
}
