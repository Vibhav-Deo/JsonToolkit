namespace JsonToolkit.STJ;

/// <summary>
/// Exception thrown when JsonPath operations encounter errors.
/// </summary>
public class JsonPathException : JsonToolkitException
{
    /// <summary>
    /// Gets the JsonPath expression that caused the error.
    /// </summary>
    public string? JsonPath { get; }

    /// <summary>
    /// Gets the position in the JsonPath expression where the error occurred.
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Initializes a new instance of the JsonPathException class.
    /// </summary>
    public JsonPathException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonPathException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JsonPathException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonPathException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public JsonPathException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonPathException class with JsonPath-specific context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="jsonPath">The JsonPath expression that caused the error.</param>
    /// <param name="position">The position in the JsonPath expression where the error occurred.</param>
    /// <param name="propertyPath">The JSON property path where the error occurred.</param>
    /// <param name="operation">The operation being performed when the error occurred.</param>
    /// <param name="sourceElement">The JSON element that caused the error.</param>
    public JsonPathException(string message, string? jsonPath = null, int position = -1, string? propertyPath = null,
        string? operation = null, JsonElement? sourceElement = null)
        : base(message, propertyPath, operation, sourceElement)
    {
        JsonPath = jsonPath;
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of the JsonPathException class with JsonPath-specific context and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="jsonPath">The JsonPath expression that caused the error.</param>
    /// <param name="position">The position in the JsonPath expression where the error occurred.</param>
    /// <param name="propertyPath">The JSON property path where the error occurred.</param>
    /// <param name="operation">The operation being performed when the error occurred.</param>
    /// <param name="sourceElement">The JSON element that caused the error.</param>
    public JsonPathException(string message, Exception innerException, string? jsonPath = null, int position = -1,
        string? propertyPath = null, string? operation = null, JsonElement? sourceElement = null)
        : base(message, innerException, propertyPath, operation, sourceElement)
    {
        JsonPath = jsonPath;
        Position = position;
    }
}