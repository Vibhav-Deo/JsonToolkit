using System;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Base exception class for all JsonToolkit.STJ exceptions.
    /// Provides enhanced error context for JSON processing operations.
    /// </summary>
    public class JsonToolkitException : JsonException
    {
        /// <summary>
        /// Gets the JSON property path where the error occurred.
        /// </summary>
        public string? PropertyPath { get; }

        /// <summary>
        /// Gets the operation being performed when the error occurred.
        /// </summary>
        public string? Operation { get; }

        /// <summary>
        /// Gets the JSON element that caused the error, if available.
        /// </summary>
        public JsonElement? SourceElement { get; }

        /// <summary>
        /// Initializes a new instance of the JsonToolkitException class.
        /// </summary>
        public JsonToolkitException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the JsonToolkitException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JsonToolkitException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JsonToolkitException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public JsonToolkitException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the JsonToolkitException class with enhanced context information.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="propertyPath">The JSON property path where the error occurred.</param>
        /// <param name="operation">The operation being performed when the error occurred.</param>
        /// <param name="sourceElement">The JSON element that caused the error.</param>
        public JsonToolkitException(string message, string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null) 
            : base(message)
        {
            PropertyPath = propertyPath;
            Operation = operation;
            SourceElement = sourceElement;
        }

        /// <summary>
        /// Initializes a new instance of the JsonToolkitException class with enhanced context information and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="propertyPath">The JSON property path where the error occurred.</param>
        /// <param name="operation">The operation being performed when the error occurred.</param>
        /// <param name="sourceElement">The JSON element that caused the error.</param>
        public JsonToolkitException(string message, Exception innerException, string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null) 
            : base(message, innerException)
        {
            PropertyPath = propertyPath;
            Operation = operation;
            SourceElement = sourceElement;
        }
    }
}