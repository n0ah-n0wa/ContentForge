namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Raised when authentication credentials or tokens are invalid.
/// </summary>
public sealed class AuthenticationFailedException : ApplicationException
{
    public AuthenticationFailedException()
        : base("Authentication failed.")
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
