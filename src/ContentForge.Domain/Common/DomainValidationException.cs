namespace ContentForge.Domain.Common;

/// <summary>
/// Thrown when an operation violates domain validation rules.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }

    public DomainValidationException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    public string? Field { get; }
}
