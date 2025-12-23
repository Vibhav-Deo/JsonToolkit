using System;

namespace JsonToolkit.STJ;

public class ValidationError
{
    public string PropertyPath { get; }
    public string Message { get; }
    public string ErrorType { get; }

    public ValidationError(string propertyPath, string message, string errorType)
    {
        PropertyPath = propertyPath ?? string.Empty;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ErrorType = errorType ?? "ValidationError";
    }

    public override string ToString() => $"{ErrorType} at '{PropertyPath}': {Message}";
}
