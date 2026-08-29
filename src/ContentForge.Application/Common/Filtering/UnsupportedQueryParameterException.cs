namespace ContentForge.Application.Common.Filtering;

using ContentForge.Application.Common.Exceptions;

/// <summary>
/// Thrown when a client supplies an unsupported filter or sort parameter.
/// </summary>
public sealed class UnsupportedQueryParameterException : ApplicationException
{
    public UnsupportedQueryParameterException(string parameterName, string message)
        : base(message)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}
