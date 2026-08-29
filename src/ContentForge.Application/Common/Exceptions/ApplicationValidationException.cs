namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Thrown when input or application validation fails.
/// </summary>
public sealed class ApplicationValidationException : ApplicationException
{
    public ApplicationValidationException(IReadOnlyList<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Failures = failures;
    }

    public ApplicationValidationException(string field, string message)
        : this([new ValidationFailure(field, message)])
    {
    }

    public IReadOnlyList<ValidationFailure> Failures { get; }
}
