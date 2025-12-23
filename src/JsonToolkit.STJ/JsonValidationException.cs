using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Exception thrown when JSON validation fails during deserialization.
    /// Contains detailed information about all validation errors that occurred.
    /// </summary>
    public class JsonValidationException : JsonToolkitException
    {
        /// <summary>
        /// Gets the collection of validation errors that caused this exception.
        /// </summary>
        public IReadOnlyList<ValidationError> ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the JsonValidationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The validation errors that occurred.</param>
        public JsonValidationException(string message, IEnumerable<ValidationError> validationErrors)
            : base(message, operation: "Validation")
        {
            ValidationErrors = validationErrors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the JsonValidationException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationErrors">The validation errors that occurred.</param>
        /// <param name="innerException">The inner exception.</param>
        public JsonValidationException(string message, IEnumerable<ValidationError> validationErrors, Exception innerException)
            : base(message, innerException, operation: "Validation")
        {
            ValidationErrors = validationErrors?.ToList() ?? new List<ValidationError>();
        }

        /// <summary>
        /// Gets a detailed error message that includes all validation errors.
        /// </summary>
        public override string Message
        {
            get
            {
                if (!ValidationErrors.Any())
                    return base.Message;

                var errorMessages = ValidationErrors.Select(e => $"  - {e}");
                return $"{base.Message}\nValidation errors:\n{string.Join("\n", errorMessages)}";
            }
        }

        /// <summary>
        /// Gets validation errors for a specific property path.
        /// </summary>
        /// <param name="propertyPath">The property path to filter by.</param>
        /// <returns>Validation errors for the specified property path.</returns>
        public IEnumerable<ValidationError> GetErrorsForProperty(string propertyPath)
        {
            return ValidationErrors.Where(e => 
                string.Equals(e.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets validation errors of a specific type.
        /// </summary>
        /// <param name="errorType">The error type to filter by.</param>
        /// <returns>Validation errors of the specified type.</returns>
        public IEnumerable<ValidationError> GetErrorsByType(string errorType)
        {
            return ValidationErrors.Where(e => 
                string.Equals(e.ErrorType, errorType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if there are any validation errors for a specific property.
        /// </summary>
        /// <param name="propertyPath">The property path to check.</param>
        /// <returns>True if there are errors for the property; otherwise, false.</returns>
        public bool HasErrorsForProperty(string propertyPath)
        {
            return ValidationErrors.Any(e => 
                string.Equals(e.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
        }
    }
}