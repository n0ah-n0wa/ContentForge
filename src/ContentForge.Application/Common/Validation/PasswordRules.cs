namespace ContentForge.Application.Common.Validation;

using FluentValidation;

/// <summary>
/// Shared password complexity rules aligned with ASP.NET Core Identity options.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 12;

    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilderInitial<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(MinimumLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
}
