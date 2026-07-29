namespace ProductApi.Domain.Exceptions;

/// <summary>
/// Thrown when FluentValidation rules fail. Named to avoid clashing with
/// FluentValidation's own ValidationException type.
/// </summary>
public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}
