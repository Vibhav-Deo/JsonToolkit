using System.Text.RegularExpressions;

namespace JsonToolkit.STJ.ValidationAttributes;

/// <summary>
/// Specifies a regular expression pattern that a property value must match.
/// Can be applied to string properties to enforce pattern matching constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class JsonPatternAttribute : JsonValidationAttribute
{
    private readonly Regex _regex;

    /// <summary>
    /// Gets the regular expression pattern.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the regular expression options used for matching.
    /// </summary>
    public RegexOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the JsonPatternAttribute class.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when pattern is invalid.</exception>
    public JsonPatternAttribute(string pattern) : this(pattern, RegexOptions.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonPatternAttribute class with options.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">The regular expression options to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when pattern is invalid.</exception>
    public JsonPatternAttribute(string pattern, RegexOptions options)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentNullException(nameof(pattern), "Pattern cannot be null or empty.");

        Pattern = pattern;
        Options = options;
        ErrorType = "PatternValidationError";

        try
        {
            _regex = new Regex(pattern, options | RegexOptions.Compiled);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid regular expression pattern: {pattern}", nameof(pattern), ex);
        }
    }

    /// <summary>
    /// Validates the specified value against the pattern constraint.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="propertyPath">The full path to the property being validated.</param>
    /// <returns>A ValidationError if validation fails, null if validation passes.</returns>
    public override ValidationError? Validate(object? value, string propertyName, string propertyPath)
    {
        if (value == null)
            return null; // Null values are not validated by pattern constraints

        // Convert value to string for pattern matching
        string stringValue;
        if (value is string str)
        {
            stringValue = str;
        }
        else
        {
            // Try to convert to string
            try
            {
                stringValue = value.ToString() ?? string.Empty;
            }
            catch
            {
                return new ValidationError(
                    propertyPath,
                    $"Property '{propertyName}' must be convertible to string for pattern validation.",
                    "PatternValidationError"
                );
            }
        }

        // Check if the string matches the pattern
        try
        {
            if (!_regex.IsMatch(stringValue))
            {
                return CreateValidationError(propertyName, propertyPath, value, typeof(string));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return new ValidationError(
                propertyPath,
                $"Pattern validation for property '{propertyName}' timed out.",
                "PatternValidationError"
            );
        }
        catch (Exception ex)
        {
            return new ValidationError(
                propertyPath,
                $"Error during pattern validation for property '{propertyName}': {ex.Message}",
                "PatternValidationError"
            );
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Gets the default error message for pattern validation.
    /// </summary>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <returns>The default error message.</returns>
    protected override string GetDefaultErrorMessage(string propertyName)
    {
        return $"Property '{propertyName}' must match the pattern '{Pattern}'.";
    }
}