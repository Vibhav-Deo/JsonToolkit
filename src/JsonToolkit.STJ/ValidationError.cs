namespace JsonToolkit.STJ;

public class ValidationError
{
    public string PropertyPath { get; }
    public string Message { get; }
    public string ErrorType { get; }
    
    /// <summary>
    /// Gets the attempted value that caused the validation error.
    /// </summary>
    public object? AttemptedValue { get; }
    
    /// <summary>
    /// Gets the expected type for the property.
    /// </summary>
    public Type? ExpectedType { get; }
    
    /// <summary>
    /// Gets the line number where the error occurred in the JSON document.
    /// </summary>
    public long? LineNumber { get; }
    
    /// <summary>
    /// Gets the byte position where the error occurred in the JSON document.
    /// </summary>
    public long? BytePositionInLine { get; }
    
    /// <summary>
    /// Gets additional context information for the validation error.
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalContext { get; }

    public ValidationError(string propertyPath, string message, string errorType)
        : this(propertyPath, message, errorType, null, null, null, null, null)
    {
    }

    public ValidationError(
        string propertyPath, 
        string message, 
        string errorType,
        object? attemptedValue = null,
        Type? expectedType = null,
        long? lineNumber = null,
        long? bytePositionInLine = null,
        Dictionary<string, object>? additionalContext = null)
    {
        PropertyPath = propertyPath ?? string.Empty;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ErrorType = errorType ?? "ValidationError";
        AttemptedValue = attemptedValue;
        ExpectedType = expectedType;
        LineNumber = lineNumber;
        BytePositionInLine = bytePositionInLine;
        AdditionalContext = additionalContext ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a ValidationError from an ErrorContext.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="errorType">The type of validation error.</param>
    /// <param name="context">The error context containing additional information.</param>
    /// <returns>A ValidationError with context information.</returns>
    public static ValidationError FromContext(string message, string errorType, ErrorContext context)
    {
        return new ValidationError(
            context.PropertyPath ?? "$",
            message,
            errorType,
            context.AttemptedValue,
            context.ExpectedType,
            context.LineNumber,
            context.BytePositionInLine,
            new Dictionary<string, object>(context.AdditionalContext)
        );
    }

    /// <summary>
    /// Creates a ValidationError from a Utf8JsonReader.
    /// </summary>
    /// <param name="propertyPath">The property path where the error occurred.</param>
    /// <param name="message">The validation error message.</param>
    /// <param name="errorType">The type of validation error.</param>
    /// <param name="reader">The reader providing line/position information.</param>
    /// <param name="attemptedValue">The value that failed validation.</param>
    /// <param name="expectedType">The expected type for the property.</param>
    /// <returns>A ValidationError with reader context information.</returns>
    public static ValidationError FromReader(
        string propertyPath,
        string message,
        string errorType,
        ref Utf8JsonReader reader,
        object? attemptedValue = null,
        Type? expectedType = null)
    {
        return new ValidationError(
            propertyPath,
            message,
            errorType,
            attemptedValue,
            expectedType,
            null, // Line number will be extracted from exception messages when available
            null  // Byte position will be extracted from exception messages when available
        );
    }

    /// <summary>
    /// Gets a detailed error message including all available context information.
    /// </summary>
    /// <returns>A comprehensive error message.</returns>
    public string GetDetailedMessage()
    {
        var parts = new List<string> { $"{ErrorType} at '{PropertyPath}': {Message}" };

        if (LineNumber.HasValue)
            parts.Add($"Line: {LineNumber.Value + 1}"); // Convert to 1-based line numbers

        if (BytePositionInLine.HasValue)
            parts.Add($"Position: {BytePositionInLine.Value}");

        if (ExpectedType != null)
            parts.Add($"Expected type: {ExpectedType.Name}");

        if (AttemptedValue != null)
            parts.Add($"Attempted value: {AttemptedValue}");

        foreach (var kvp in AdditionalContext)
            parts.Add($"{kvp.Key}: {kvp.Value}");

        return string.Join(". ", parts);
    }

    public override string ToString() => GetDetailedMessage();
}
