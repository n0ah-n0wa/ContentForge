namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// </summary>
public sealed class NotFoundApplicationException : ApplicationException
{
    public NotFoundApplicationException(string resourceName, object resourceId)
        : base($"{resourceName} '{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    public string ResourceName { get; }

    public object ResourceId { get; }
}
