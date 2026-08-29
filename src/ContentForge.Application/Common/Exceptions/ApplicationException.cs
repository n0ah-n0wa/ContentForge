namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Base exception for application-layer failures.
/// </summary>
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message)
        : base(message)
    {
    }

    protected ApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
