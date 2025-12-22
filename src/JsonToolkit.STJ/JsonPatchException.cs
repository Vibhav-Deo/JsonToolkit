using System;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Exception thrown when JSON Patch operations encounter errors.
    /// </summary>
    public class JsonPatchException : JsonToolkitException
    {
        /// <summary>
        /// Gets the JSON Patch operation that failed.
        /// </summary>
        public JsonPatchOperation? FailedOperation { get; }

        /// <summary>
        /// Gets the index of the operation that failed in the patch document.
        /// </summary>
        public int OperationIndex { get; }

        /// <summary>
        /// Initializes a new instance of the JsonPatchException class.
        /// </summary>
        public JsonPatchException() : base()
        {
            OperationIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the JsonPatchException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JsonPatchException(string message) : base(message)
        {
            OperationIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the JsonPatchException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public JsonPatchException(string message, Exception innerException) : base(message, innerException)
        {
            OperationIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the JsonPatchException class with JSON Patch-specific context.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="failedOperation">The JSON Patch operation that failed.</param>
        /// <param name="operationIndex">The index of the operation that failed.</param>
        /// <param name="propertyPath">The JSON property path where the error occurred.</param>
        /// <param name="operation">The operation being performed when the error occurred.</param>
        /// <param name="sourceElement">The JSON element that caused the error.</param>
        public JsonPatchException(string message, JsonPatchOperation? failedOperation = null, int operationIndex = -1, string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null)
            : base(message, propertyPath, operation, sourceElement)
        {
            FailedOperation = failedOperation;
            OperationIndex = operationIndex;
        }

        /// <summary>
        /// Initializes a new instance of the JsonPatchException class with JSON Patch-specific context and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="failedOperation">The JSON Patch operation that failed.</param>
        /// <param name="operationIndex">The index of the operation that failed.</param>
        /// <param name="propertyPath">The JSON property path where the error occurred.</param>
        /// <param name="operation">The operation being performed when the error occurred.</param>
        /// <param name="sourceElement">The JSON element that caused the error.</param>
        public JsonPatchException(string message, Exception innerException, JsonPatchOperation? failedOperation = null, int operationIndex = -1, string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null)
            : base(message, innerException, propertyPath, operation, sourceElement)
        {
            FailedOperation = failedOperation;
            OperationIndex = operationIndex;
        }
    }

    /// <summary>
    /// Represents a JSON Patch operation.
    /// </summary>
    public class JsonPatchOperation
    {
        /// <summary>
        /// Gets or sets the operation type (add, remove, replace, move, copy, test).
        /// </summary>
        public string Op { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target path for the operation.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value for the operation (used in add, replace, test).
        /// </summary>
        public JsonElement? Value { get; set; }

        /// <summary>
        /// Gets or sets the source path for move and copy operations.
        /// </summary>
        public string? From { get; set; }
    }
}
