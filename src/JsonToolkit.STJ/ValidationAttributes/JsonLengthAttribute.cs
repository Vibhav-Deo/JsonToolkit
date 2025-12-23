using System;
using System.Collections;

namespace JsonToolkit.STJ.ValidationAttributes
{
    /// <summary>
    /// Specifies the length constraints for a property value.
    /// Can be applied to string properties, arrays, or collections to enforce minimum and maximum length constraints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonLengthAttribute : JsonValidationAttribute
    {
        /// <summary>
        /// Gets the minimum allowed length.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Gets the maximum allowed length.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Initializes a new instance of the JsonLengthAttribute class.
        /// </summary>
        /// <param name="minLength">The minimum allowed length.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <exception cref="ArgumentException">Thrown when minLength is negative or greater than maxLength.</exception>
        public JsonLengthAttribute(int minLength, int maxLength)
        {
            if (minLength < 0)
                throw new ArgumentException("Minimum length cannot be negative.", nameof(minLength));
            
            if (minLength > maxLength)
                throw new ArgumentException($"Minimum length ({minLength}) cannot be greater than maximum length ({maxLength}).");

            MinLength = minLength;
            MaxLength = maxLength;
            ErrorType = "LengthValidationError";
        }

        /// <summary>
        /// Initializes a new instance of the JsonLengthAttribute class with only a maximum length.
        /// </summary>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <exception cref="ArgumentException">Thrown when maxLength is negative.</exception>
        public JsonLengthAttribute(int maxLength) : this(0, maxLength)
        {
        }

        /// <summary>
        /// Validates the specified value against the length constraints.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <param name="propertyPath">The full path to the property being validated.</param>
        /// <returns>A ValidationError if validation fails, null if validation passes.</returns>
        public override ValidationError? Validate(object? value, string propertyName, string propertyPath)
        {
            if (value == null)
                return null; // Null values are not validated by length constraints

            int length;

            // Determine the length based on the value type
            switch (value)
            {
                case string stringValue:
                    length = stringValue.Length;
                    break;
                
                case ICollection collection:
                    length = collection.Count;
                    break;
                
                case IEnumerable enumerable:
                    // Count items in enumerable (less efficient but works for any IEnumerable)
                    length = 0;
                    foreach (var _ in enumerable)
                        length++;
                    break;
                
                default:
                    return new ValidationError(
                        propertyPath,
                        $"Property '{propertyName}' must be a string, array, or collection for length validation.",
                        "LengthValidationError"
                    );
            }

            // Check minimum length constraint
            if (length < MinLength)
            {
                return CreateValidationError(propertyName, propertyPath);
            }

            // Check maximum length constraint
            if (length > MaxLength)
            {
                return CreateValidationError(propertyName, propertyPath);
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Gets the default error message for length validation.
        /// </summary>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>The default error message.</returns>
        protected override string GetDefaultErrorMessage(string propertyName)
        {
            if (MinLength == 0)
            {
                return $"Property '{propertyName}' must have a length of at most {MaxLength}.";
            }
            else if (MinLength == MaxLength)
            {
                return $"Property '{propertyName}' must have a length of exactly {MinLength}.";
            }
            else
            {
                return $"Property '{propertyName}' must have a length between {MinLength} and {MaxLength}.";
            }
        }
    }
}