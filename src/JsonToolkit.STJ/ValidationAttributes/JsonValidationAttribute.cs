using System;

namespace JsonToolkit.STJ.ValidationAttributes
{
    /// <summary>
    /// Base class for JSON validation attributes that can be applied to properties
    /// to enforce constraints during deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public abstract class JsonValidationAttribute : Attribute, IJsonValidationAttribute
    {
        /// <summary>
        /// Gets or sets the error message to use when validation fails.
        /// If not specified, a default message will be generated.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the error type identifier for this validation.
        /// </summary>
        public string ErrorType { get; set; } = "ValidationError";

        /// <summary>
        /// Validates the specified value against this attribute's constraints.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <param name="propertyPath">The full path to the property being validated.</param>
        /// <returns>A ValidationError if validation fails, null if validation passes.</returns>
        public abstract ValidationError? Validate(object? value, string propertyName, string propertyPath);

        /// <summary>
        /// Gets the default error message for this validation attribute.
        /// </summary>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <returns>The default error message.</returns>
        protected abstract string GetDefaultErrorMessage(string propertyName);

        /// <summary>
        /// Creates a ValidationError with the appropriate message.
        /// </summary>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <param name="propertyPath">The full path to the property being validated.</param>
        /// <returns>A ValidationError instance.</returns>
        protected ValidationError CreateValidationError(string propertyName, string propertyPath)
        {
            var message = ErrorMessage ?? GetDefaultErrorMessage(propertyName);
            return new ValidationError(propertyPath, message, ErrorType);
        }
    }
}