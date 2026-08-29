namespace ContentForge.Application.Common.Exceptions;

/// <summary>
/// Structured validation failure for application and input validation.
/// </summary>
public sealed record ValidationFailure(string Field, string Message);
