using System.Collections.Generic;
using System.Linq;

namespace JsonToolkit.STJ;

public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<ValidationError> Errors { get; }

    private ValidationResult(bool isValid, IEnumerable<ValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors?.ToList() ?? new List<ValidationError>();
    }

    public static ValidationResult Success() => new ValidationResult(true, new List<ValidationError>());

    public static ValidationResult Failure(params ValidationError[] errors) => new ValidationResult(false, errors);

    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new ValidationResult(false, errors);
}
