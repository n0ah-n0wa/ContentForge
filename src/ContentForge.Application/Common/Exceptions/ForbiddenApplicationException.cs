namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Thrown when the current user lacks permission for an operation.
/// </summary>
public sealed class ForbiddenApplicationException : ApplicationException
{
    public ForbiddenApplicationException(string message)
        : base(message)
    {
    }
}
