namespace JsonToolkit.STJ.ValidationAttributes;

/// <summary>
/// Interface for custom JSON validation attributes.
/// Implement this interface to create custom validation logic that can be applied to properties.
/// </summary>
public interface IJsonValidationAttribute
{
    /// <summary>
    /// Gets the error type identifier for this validation.
    /// </summary>
    string ErrorType { get; }

    /// <summary>
    /// Gets the custom error message, if specified.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Validates the specified value against this attribute's constraints.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="propertyPath">The full path to the property being validated.</param>
    /// <returns>A ValidationError if validation fails, null if validation passes.</returns>
    ValidationError? Validate(object? value, string propertyName, string propertyPath);
}