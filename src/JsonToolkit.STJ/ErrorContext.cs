using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Provides comprehensive error context information for JSON processing operations.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// Gets or sets the JSON property path where the error occurred.
        /// </summary>
        public string? PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the operation being performed when the error occurred.
        /// </summary>
        public string? Operation { get; set; }

        /// <summary>
        /// Gets or sets the JSON element that caused the error, if available.
        /// </summary>
        public JsonElement? SourceElement { get; set; }

        /// <summary>
        /// Gets or sets the line number where the error occurred in the JSON document.
        /// </summary>
        public long? LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the byte position where the error occurred in the JSON document.
        /// </summary>
        public long? BytePositionInLine { get; set; }

        /// <summary>
        /// Gets or sets the expected type for type conversion errors.
        /// </summary>
        public Type? ExpectedType { get; set; }

        /// <summary>
        /// Gets or sets the actual type encountered for type conversion errors.
        /// </summary>
        public Type? ActualType { get; set; }

        /// <summary>
        /// Gets or sets the attempted value that caused the error.
        /// </summary>
        public object? AttemptedValue { get; set; }

        /// <summary>
        /// Gets or sets the converter that was being used when the error occurred.
        /// </summary>
        public string? ConverterName { get; set; }

        /// <summary>
        /// Gets or sets additional context properties for the error.
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; } = new();

        /// <summary>
        /// Creates an ErrorContext from a Utf8JsonReader.
        /// </summary>
        /// <param name="reader">The reader to extract context from.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <returns>An ErrorContext with reader information.</returns>
        public static ErrorContext FromReader(ref Utf8JsonReader reader, string? operation = null)
        {
            return new ErrorContext
            {
                Operation = operation,
                LineNumber = null, // Will be extracted from exception messages when available
                BytePositionInLine = null, // Will be extracted from exception messages when available
                PropertyPath = null // Will be tracked separately by PropertyPathTracker
            };
        }

        /// <summary>
        /// Creates an ErrorContext for a type conversion error.
        /// </summary>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="actualType">The actual type encountered.</param>
        /// <param name="attemptedValue">The value that failed conversion.</param>
        /// <param name="propertyPath">The property path where the error occurred.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <returns>An ErrorContext for the type conversion error.</returns>
        public static ErrorContext ForTypeConversion(Type expectedType, Type? actualType = null, object? attemptedValue = null, string? propertyPath = null, string? operation = null)
        {
            return new ErrorContext
            {
                ExpectedType = expectedType,
                ActualType = actualType,
                AttemptedValue = attemptedValue,
                PropertyPath = propertyPath,
                Operation = operation ?? "TypeConversion"
            };
        }

        /// <summary>
        /// Creates an ErrorContext for a converter error.
        /// </summary>
        /// <param name="converterName">The name of the converter that failed.</param>
        /// <param name="propertyPath">The property path where the error occurred.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="sourceElement">The JSON element being processed.</param>
        /// <returns>An ErrorContext for the converter error.</returns>
        public static ErrorContext ForConverter(string converterName, string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null)
        {
            return new ErrorContext
            {
                ConverterName = converterName,
                PropertyPath = propertyPath,
                Operation = operation ?? "Conversion",
                SourceElement = sourceElement
            };
        }

        /// <summary>
        /// Creates an ErrorContext for a validation error.
        /// </summary>
        /// <param name="propertyPath">The property path where validation failed.</param>
        /// <param name="attemptedValue">The value that failed validation.</param>
        /// <param name="operation">The validation operation.</param>
        /// <returns>An ErrorContext for the validation error.</returns>
        public static ErrorContext ForValidation(string propertyPath, object? attemptedValue = null, string? operation = null)
        {
            return new ErrorContext
            {
                PropertyPath = propertyPath,
                AttemptedValue = attemptedValue,
                Operation = operation ?? "Validation"
            };
        }

        /// <summary>
        /// Adds additional context information.
        /// </summary>
        /// <param name="key">The context key.</param>
        /// <param name="value">The context value.</param>
        /// <returns>This ErrorContext for method chaining.</returns>
        public ErrorContext WithContext(string key, object value)
        {
            AdditionalContext[key] = value;
            return this;
        }

        /// <summary>
        /// Gets a formatted error message with all available context information.
        /// </summary>
        /// <param name="baseMessage">The base error message.</param>
        /// <returns>A comprehensive error message with context.</returns>
        public string GetFormattedMessage(string baseMessage)
        {
            var parts = new List<string> { baseMessage };

            if (!string.IsNullOrEmpty(PropertyPath))
                parts.Add($"Property path: {PropertyPath}");

            if (!string.IsNullOrEmpty(Operation))
                parts.Add($"Operation: {Operation}");

            if (LineNumber.HasValue)
                parts.Add($"Line: {LineNumber.Value + 1}"); // Convert to 1-based line numbers

            if (BytePositionInLine.HasValue)
                parts.Add($"Position: {BytePositionInLine.Value}");

            if (ExpectedType != null)
                parts.Add($"Expected type: {ExpectedType.Name}");

            if (ActualType != null)
                parts.Add($"Actual type: {ActualType.Name}");

            if (AttemptedValue != null)
                parts.Add($"Attempted value: {AttemptedValue}");

            if (!string.IsNullOrEmpty(ConverterName))
                parts.Add($"Converter: {ConverterName}");

            foreach (var kvp in AdditionalContext)
                parts.Add($"{kvp.Key}: {kvp.Value}");

            return string.Join(". ", parts);
        }

    }
}