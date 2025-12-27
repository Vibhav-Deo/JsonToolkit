using System.Globalization;

namespace JsonToolkit.STJ.ValidationAttributes;

/// <summary>
/// Specifies the numeric range constraints for a property value.
/// Can be applied to numeric properties to enforce minimum and maximum value constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class JsonRangeAttribute : JsonValidationAttribute
{
    /// <summary>
    /// Gets the minimum allowed value.
    /// </summary>
    public double Minimum { get; }

    /// <summary>
    /// Gets the maximum allowed value.
    /// </summary>
    public double Maximum { get; }

    /// <summary>
    /// Gets or sets whether the minimum value is inclusive (default: true).
    /// </summary>
    public bool MinimumIsInclusive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the maximum value is inclusive (default: true).
    /// </summary>
    public bool MaximumIsInclusive { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the JsonRangeAttribute class.
    /// </summary>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <exception cref="ArgumentException">Thrown when minimum is greater than maximum.</exception>
    public JsonRangeAttribute(double minimum, double maximum)
    {
        if (minimum > maximum)
            throw new ArgumentException($"Minimum value ({minimum}) cannot be greater than maximum value ({maximum}).");

        Minimum = minimum;
        Maximum = maximum;
        ErrorType = "RangeValidationError";
    }

    /// <summary>
    /// Initializes a new instance of the JsonRangeAttribute class with integer values.
    /// </summary>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <exception cref="ArgumentException">Thrown when minimum is greater than maximum.</exception>
    public JsonRangeAttribute(int minimum, int maximum) : this((double)minimum, (double)maximum)
    {
    }

    /// <summary>
    /// Validates the specified value against the range constraints.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="propertyPath">The full path to the property being validated.</param>
    /// <returns>A ValidationError if validation fails, null if validation passes.</returns>
    public override ValidationError? Validate(object? value, string propertyName, string propertyPath)
    {
        if (value == null)
            return null; // Null values are not validated by range constraints

        // Convert value to double for comparison
        double numericValue;
        try
        {
            numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
        {
            return new ValidationError(
                propertyPath,
                $"Property '{propertyName}' must be a numeric value for range validation.",
                "RangeValidationError"
            );
        }

        // Check minimum constraint
        if (MinimumIsInclusive ? numericValue < Minimum : numericValue <= Minimum)
        {
            return CreateValidationError(propertyName, propertyPath, value, typeof(double));
        }

        // Check maximum constraint
        if (MaximumIsInclusive ? numericValue > Maximum : numericValue >= Maximum)
        {
            return CreateValidationError(propertyName, propertyPath, value, typeof(double));
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Gets the default error message for range validation.
    /// </summary>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <returns>The default error message.</returns>
    protected override string GetDefaultErrorMessage(string propertyName)
    {
        var minOperator = MinimumIsInclusive ? ">=" : ">";
        var maxOperator = MaximumIsInclusive ? "<=" : "<";

        return $"Property '{propertyName}' must be {minOperator} {Minimum} and {maxOperator} {Maximum}.";
    }
}