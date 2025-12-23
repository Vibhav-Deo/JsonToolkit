using System;
using System.Text.Json;

namespace JsonToolkit.STJ.Extensions
{
    /// <summary>
    /// Extension methods for enhanced error handling and diagnostics.
    /// </summary>
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        /// Wraps an exception with enhanced JsonToolkit context information.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="context">The error context to include.</param>
        /// <param name="message">Optional custom message (uses original message if null).</param>
        /// <returns>A JsonToolkitException with enhanced context.</returns>
        public static JsonToolkitException WithContext(this Exception exception, ErrorContext context, string? message = null)
        {
            var enhancedMessage = message ?? context.GetFormattedMessage(exception.Message);
            
            return new JsonToolkitException(
                enhancedMessage,
                exception,
                context.PropertyPath,
                context.Operation,
                context.SourceElement
            );
        }

        /// <summary>
        /// Creates a JsonToolkitException from a reader context.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="reader">The reader providing context information.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>A JsonToolkitException with reader context.</returns>
        public static JsonToolkitException WithReaderContext(this Exception exception, ref Utf8JsonReader reader, string? operation = null, string? message = null)
        {
            var context = ErrorContext.FromReader(ref reader, operation);
            return exception.WithContext(context, message);
        }

        /// <summary>
        /// Creates a JsonToolkitException for type conversion errors.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="actualType">The actual type encountered.</param>
        /// <param name="attemptedValue">The value that failed conversion.</param>
        /// <param name="propertyPath">The property path where the error occurred.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>A JsonToolkitException with type conversion context.</returns>
        public static JsonToolkitException WithTypeConversionContext(
            this Exception exception,
            Type expectedType,
            Type? actualType = null,
            object? attemptedValue = null,
            string? propertyPath = null,
            string? message = null)
        {
            var context = ErrorContext.ForTypeConversion(expectedType, actualType, attemptedValue, propertyPath);
            return exception.WithContext(context, message);
        }

        /// <summary>
        /// Creates a JsonToolkitException for converter errors.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="converterName">The name of the converter that failed.</param>
        /// <param name="propertyPath">The property path where the error occurred.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="message">Optional custom message.</param>
        /// <returns>A JsonToolkitException with converter context.</returns>
        public static JsonToolkitException WithConverterContext(
            this Exception exception,
            string converterName,
            string? propertyPath = null,
            string? operation = null,
            string? message = null)
        {
            var context = ErrorContext.ForConverter(converterName, propertyPath, operation);
            return exception.WithContext(context, message);
        }

        /// <summary>
        /// Gets comprehensive diagnostic information for an exception.
        /// </summary>
        /// <param name="exception">The exception to analyze.</param>
        /// <param name="options">The JsonSerializerOptions that were in use.</param>
        /// <returns>Detailed diagnostic information.</returns>
        public static ErrorDiagnosticResult GetDiagnostics(this Exception exception, JsonSerializerOptions? options = null)
        {
            return ErrorDiagnostics.AnalyzeException(exception, options);
        }

        /// <summary>
        /// Creates a comprehensive error report for debugging.
        /// </summary>
        /// <param name="exception">The exception to report on.</param>
        /// <param name="options">The JsonSerializerOptions that were in use.</param>
        /// <param name="jsonInput">The JSON input that caused the error (optional).</param>
        /// <param name="targetType">The target type being processed (optional).</param>
        /// <returns>A formatted error report.</returns>
        public static string CreateErrorReport(
            this Exception exception,
            JsonSerializerOptions? options = null,
            string? jsonInput = null,
            Type? targetType = null)
        {
            return ErrorDiagnostics.CreateErrorReport(exception, options, jsonInput, targetType);
        }

        /// <summary>
        /// Checks if an exception is a JsonToolkit.STJ exception.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception is a JsonToolkitException; otherwise, false.</returns>
        public static bool IsJsonToolkitException(this Exception exception)
        {
            return exception is JsonToolkitException;
        }

        /// <summary>
        /// Checks if an exception is a validation-related exception.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception is validation-related; otherwise, false.</returns>
        public static bool IsValidationException(this Exception exception)
        {
            return exception is JsonValidationException;
        }

        /// <summary>
        /// Gets the property path from a JsonToolkitException, if available.
        /// </summary>
        /// <param name="exception">The exception to extract the path from.</param>
        /// <returns>The property path or null if not available.</returns>
        public static string? GetPropertyPath(this Exception exception)
        {
            return exception is JsonToolkitException toolkitEx ? toolkitEx.PropertyPath : null;
        }

        /// <summary>
        /// Gets the operation from a JsonToolkitException, if available.
        /// </summary>
        /// <param name="exception">The exception to extract the operation from.</param>
        /// <returns>The operation or null if not available.</returns>
        public static string? GetOperation(this Exception exception)
        {
            return exception is JsonToolkitException toolkitEx ? toolkitEx.Operation : null;
        }

        /// <summary>
        /// Safely executes a JSON operation and provides enhanced error context on failure.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for error reporting.</param>
        /// <param name="propertyPath">The property path context for error reporting.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="JsonToolkitException">Thrown if the operation fails with enhanced context.</exception>
        public static T ExecuteWithErrorContext<T>(Func<T> operation, string operationName, string? propertyPath = null)
        {
            try
            {
                return operation();
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                var context = new ErrorContext
                {
                    Operation = operationName,
                    PropertyPath = propertyPath
                };

                throw ex.WithContext(context);
            }
        }

        /// <summary>
        /// Safely executes a JSON operation and provides enhanced error context on failure.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for error reporting.</param>
        /// <param name="propertyPath">The property path context for error reporting.</param>
        /// <exception cref="JsonToolkitException">Thrown if the operation fails with enhanced context.</exception>
        public static void ExecuteWithErrorContext(Action operation, string operationName, string? propertyPath = null)
        {
            try
            {
                operation();
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                var context = new ErrorContext
                {
                    Operation = operationName,
                    PropertyPath = propertyPath
                };

                throw ex.WithContext(context);
            }
        }
    }
}