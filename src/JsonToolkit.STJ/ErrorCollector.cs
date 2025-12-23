using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Collects and manages multiple errors during JSON processing operations.
    /// Provides functionality to accumulate errors and create comprehensive exception reports.
    /// </summary>
    public class ErrorCollector
    {
        private readonly List<CollectedError> _errors = new();
        private readonly PropertyPathTracker _pathTracker = new();

        /// <summary>
        /// Gets all collected errors.
        /// </summary>
        public IReadOnlyList<CollectedError> Errors => _errors;

        /// <summary>
        /// Gets a value indicating whether any errors have been collected.
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>
        /// Gets the number of errors collected.
        /// </summary>
        public int ErrorCount => _errors.Count;

        /// <summary>
        /// Gets the current property path from the path tracker.
        /// </summary>
        public string CurrentPath => _pathTracker.CurrentPath;

        /// <summary>
        /// Adds a validation error to the collection.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorType">The type of validation error.</param>
        /// <param name="attemptedValue">The value that caused the error.</param>
        /// <param name="propertyPath">The property path (uses current path if null).</param>
        public void AddValidationError(string message, string errorType = "ValidationError", object? attemptedValue = null, string? propertyPath = null)
        {
            var error = new CollectedError
            {
                ErrorType = ErrorType.Validation,
                Message = message,
                PropertyPath = propertyPath ?? _pathTracker.CurrentPath,
                ValidationErrorType = errorType,
                AttemptedValue = attemptedValue,
                Timestamp = DateTime.UtcNow
            };

            _errors.Add(error);
        }

        /// <summary>
        /// Adds a type conversion error to the collection.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="actualType">The actual type encountered.</param>
        /// <param name="attemptedValue">The value that failed conversion.</param>
        /// <param name="propertyPath">The property path (uses current path if null).</param>
        public void AddTypeConversionError(string message, Type expectedType, Type? actualType = null, object? attemptedValue = null, string? propertyPath = null)
        {
            var error = new CollectedError
            {
                ErrorType = ErrorType.TypeConversion,
                Message = message,
                PropertyPath = propertyPath ?? _pathTracker.CurrentPath,
                ExpectedType = expectedType,
                ActualType = actualType,
                AttemptedValue = attemptedValue,
                Timestamp = DateTime.UtcNow
            };

            _errors.Add(error);
        }

        /// <summary>
        /// Adds a converter error to the collection.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="converterName">The name of the converter that failed.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="innerException">The inner exception that caused the error.</param>
        /// <param name="propertyPath">The property path (uses current path if null).</param>
        public void AddConverterError(string message, string converterName, string? operation = null, Exception? innerException = null, string? propertyPath = null)
        {
            var error = new CollectedError
            {
                ErrorType = ErrorType.Converter,
                Message = message,
                PropertyPath = propertyPath ?? _pathTracker.CurrentPath,
                ConverterName = converterName,
                Operation = operation,
                InnerException = innerException,
                Timestamp = DateTime.UtcNow
            };

            _errors.Add(error);
        }

        /// <summary>
        /// Adds a parsing error to the collection.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="reader">The reader where the error occurred.</param>
        /// <param name="innerException">The inner exception that caused the error.</param>
        public void AddParsingError(string message, ref Utf8JsonReader reader, Exception? innerException = null)
        {
            var error = new CollectedError
            {
                ErrorType = ErrorType.Parsing,
                Message = message,
                PropertyPath = _pathTracker.CurrentPath,
                LineNumber = null, // Will be extracted from exception messages when available
                BytePositionInLine = null, // Will be extracted from exception messages when available
                InnerException = innerException,
                Timestamp = DateTime.UtcNow
            };

            _errors.Add(error);
        }

        /// <summary>
        /// Adds a general error to the collection.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="innerException">The inner exception that caused the error.</param>
        /// <param name="propertyPath">The property path (uses current path if null).</param>
        public void AddError(string message, string? operation = null, Exception? innerException = null, string? propertyPath = null)
        {
            var error = new CollectedError
            {
                ErrorType = ErrorType.General,
                Message = message,
                PropertyPath = propertyPath ?? _pathTracker.CurrentPath,
                Operation = operation,
                InnerException = innerException,
                Timestamp = DateTime.UtcNow
            };

            _errors.Add(error);
        }

        /// <summary>
        /// Pushes a property name onto the path tracker.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>A disposable that will pop the property when disposed.</returns>
        public IDisposable PushProperty(string propertyName)
        {
            return _pathTracker.PushProperty(propertyName);
        }

        /// <summary>
        /// Pushes an array index onto the path tracker.
        /// </summary>
        /// <param name="index">The array index.</param>
        /// <returns>A disposable that will pop the index when disposed.</returns>
        public IDisposable PushIndex(int index)
        {
            return _pathTracker.PushIndex(index);
        }

        /// <summary>
        /// Pushes a dictionary key onto the path tracker.
        /// </summary>
        /// <param name="key">The dictionary key.</param>
        /// <returns>A disposable that will pop the key when disposed.</returns>
        public IDisposable PushKey(string key)
        {
            return _pathTracker.PushKey(key);
        }

        /// <summary>
        /// Gets errors for a specific property path.
        /// </summary>
        /// <param name="propertyPath">The property path to filter by.</param>
        /// <returns>Errors for the specified property path.</returns>
        public IEnumerable<CollectedError> GetErrorsForProperty(string propertyPath)
        {
            return _errors.Where(e => string.Equals(e.PropertyPath, propertyPath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets errors of a specific type.
        /// </summary>
        /// <param name="errorType">The error type to filter by.</param>
        /// <returns>Errors of the specified type.</returns>
        public IEnumerable<CollectedError> GetErrorsByType(ErrorType errorType)
        {
            return _errors.Where(e => e.ErrorType == errorType);
        }

        /// <summary>
        /// Gets validation errors of a specific validation type.
        /// </summary>
        /// <param name="validationErrorType">The validation error type to filter by.</param>
        /// <returns>Validation errors of the specified type.</returns>
        public IEnumerable<CollectedError> GetValidationErrorsByType(string validationErrorType)
        {
            return _errors.Where(e => e.ErrorType == ErrorType.Validation && 
                                     string.Equals(e.ValidationErrorType, validationErrorType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Clears all collected errors.
        /// </summary>
        public void Clear()
        {
            _errors.Clear();
            _pathTracker.Clear();
        }

        /// <summary>
        /// Creates a JsonValidationException with all collected validation errors.
        /// </summary>
        /// <param name="message">The base error message.</param>
        /// <returns>A JsonValidationException containing all validation errors.</returns>
        public JsonValidationException CreateValidationException(string message = "JSON validation failed")
        {
            var validationErrors = _errors
                .Where(e => e.ErrorType == ErrorType.Validation)
                .Select(e => new ValidationError(e.PropertyPath ?? "$", e.Message, e.ValidationErrorType ?? "ValidationError"))
                .ToList();

            return new JsonValidationException(message, validationErrors);
        }

        /// <summary>
        /// Creates a JsonToolkitException with all collected errors.
        /// </summary>
        /// <param name="message">The base error message.</param>
        /// <returns>A JsonToolkitException containing error summary.</returns>
        public JsonToolkitException CreateException(string message = "Multiple JSON processing errors occurred")
        {
            var errorSummary = GetErrorSummary();
            var fullMessage = $"{message}\n{errorSummary}";
            
            var firstError = _errors.FirstOrDefault();
            return new JsonToolkitException(
                fullMessage,
                propertyPath: firstError?.PropertyPath,
                operation: firstError?.Operation ?? "MultipleOperations"
            );
        }

        /// <summary>
        /// Gets a summary of all collected errors.
        /// </summary>
        /// <returns>A formatted string summarizing all errors.</returns>
        public string GetErrorSummary()
        {
            if (!HasErrors)
                return "No errors collected.";

            var summary = new List<string>
            {
                $"Total errors: {ErrorCount}"
            };

            var errorsByType = _errors.GroupBy(e => e.ErrorType);
            foreach (var group in errorsByType)
            {
                summary.Add($"  {group.Key}: {group.Count()}");
            }

            summary.Add("Errors:");
            foreach (var error in _errors)
            {
                summary.Add($"  - {error}");
            }

            return string.Join("\n", summary);
        }
    }

    /// <summary>
    /// Represents a collected error with comprehensive context information.
    /// </summary>
    public class CollectedError
    {
        /// <summary>
        /// Gets or sets the type of error.
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property path where the error occurred.
        /// </summary>
        public string? PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the operation being performed when the error occurred.
        /// </summary>
        public string? Operation { get; set; }

        /// <summary>
        /// Gets or sets the validation error type (for validation errors).
        /// </summary>
        public string? ValidationErrorType { get; set; }

        /// <summary>
        /// Gets or sets the expected type (for type conversion errors).
        /// </summary>
        public Type? ExpectedType { get; set; }

        /// <summary>
        /// Gets or sets the actual type encountered (for type conversion errors).
        /// </summary>
        public Type? ActualType { get; set; }

        /// <summary>
        /// Gets or sets the attempted value that caused the error.
        /// </summary>
        public object? AttemptedValue { get; set; }

        /// <summary>
        /// Gets or sets the converter name (for converter errors).
        /// </summary>
        public string? ConverterName { get; set; }

        /// <summary>
        /// Gets or sets the line number where the error occurred.
        /// </summary>
        public long? LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the byte position where the error occurred.
        /// </summary>
        public long? BytePositionInLine { get; set; }

        /// <summary>
        /// Gets or sets the inner exception that caused this error.
        /// </summary>
        public Exception? InnerException { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error was collected.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Returns a string representation of the error.
        /// </summary>
        /// <returns>A formatted error string.</returns>
        public override string ToString()
        {
            var parts = new List<string> { Message };

            if (!string.IsNullOrEmpty(PropertyPath))
                parts.Add($"at '{PropertyPath}'");

            if (LineNumber.HasValue)
                parts.Add($"line {LineNumber.Value + 1}");

            if (BytePositionInLine.HasValue)
                parts.Add($"position {BytePositionInLine.Value}");

            if (ExpectedType != null)
                parts.Add($"expected {ExpectedType.Name}");

            if (ActualType != null)
                parts.Add($"got {ActualType.Name}");

            if (!string.IsNullOrEmpty(ConverterName))
                parts.Add($"converter: {ConverterName}");

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Enumeration of error types that can be collected.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// General error type.
        /// </summary>
        General,

        /// <summary>
        /// Validation error.
        /// </summary>
        Validation,

        /// <summary>
        /// Type conversion error.
        /// </summary>
        TypeConversion,

        /// <summary>
        /// Converter error.
        /// </summary>
        Converter,

        /// <summary>
        /// JSON parsing error.
        /// </summary>
        Parsing
    }
}