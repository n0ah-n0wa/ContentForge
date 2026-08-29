namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Thrown when an operation requires authentication but no valid user context is present.
/// </summary>
public sealed class UnauthorizedApplicationException : ApplicationException
{
    public UnauthorizedApplicationException(string message = "Authentication is required.")
        : base(message)
    {
    }
}
