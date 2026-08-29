namespace ContentForge.Application.Common;

using ContentForge.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;

/// <summary>
/// Runs FluentValidation validators and maps failures to application exceptions.
/// </summary>
public static class CommandValidator
{
    public static async Task EnsureValidAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if (result.IsValid)
        {
            return;
        }

        throw ToApplicationException(result);
    }

    public static ApplicationValidationException ToApplicationException(ValidationResult result)
    {
        var first = result.Errors[0];
        return new ApplicationValidationException(first.PropertyName, first.ErrorMessage);
    }
}
