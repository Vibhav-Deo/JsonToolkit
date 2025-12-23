namespace JsonToolkit.STJ
{
    /// <summary>
    /// Configuration options for JSON validation during deserialization.
    /// </summary>
    public class ValidationOptions
    {
        /// <summary>
        /// Gets or sets whether validation is enabled during deserialization.
        /// Default is true.
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to throw an exception when validation fails.
        /// If false, validation errors will be collected but not thrown.
        /// Default is true.
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect validation errors for later retrieval.
        /// This is useful when ThrowOnValidationFailure is false.
        /// Default is false.
        /// </summary>
        public bool CollectValidationErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate all properties or stop at the first error.
        /// If true, all properties will be validated and all errors collected.
        /// If false, validation stops at the first error for better performance.
        /// Default is true.
        /// </summary>
        public bool ValidateAllProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate nested objects recursively.
        /// Default is true.
        /// </summary>
        public bool ValidateNestedObjects { get; set; } = true;

        /// <summary>
        /// Creates a new ValidationOptions instance with validation enabled.
        /// </summary>
        /// <returns>A new ValidationOptions instance.</returns>
        public static ValidationOptions Enabled() => new ValidationOptions
        {
            EnableValidation = true,
            ThrowOnValidationFailure = true,
            CollectValidationErrors = false
        };

        /// <summary>
        /// Creates a new ValidationOptions instance with validation disabled for performance scenarios.
        /// </summary>
        /// <returns>A new ValidationOptions instance with validation disabled.</returns>
        public static ValidationOptions Disabled() => new ValidationOptions
        {
            EnableValidation = false,
            ThrowOnValidationFailure = false,
            CollectValidationErrors = false
        };

        /// <summary>
        /// Creates a new ValidationOptions instance that collects errors without throwing.
        /// </summary>
        /// <returns>A new ValidationOptions instance that collects errors.</returns>
        public static ValidationOptions CollectErrors() => new ValidationOptions
        {
            EnableValidation = true,
            ThrowOnValidationFailure = false,
            CollectValidationErrors = true
        };

        /// <summary>
        /// Creates a new ValidationOptions instance optimized for performance.
        /// Validation is enabled but stops at the first error.
        /// </summary>
        /// <returns>A new ValidationOptions instance optimized for performance.</returns>
        public static ValidationOptions FastFail() => new ValidationOptions
        {
            EnableValidation = true,
            ThrowOnValidationFailure = true,
            CollectValidationErrors = false,
            ValidateAllProperties = false,
            ValidateNestedObjects = false
        };
    }
}