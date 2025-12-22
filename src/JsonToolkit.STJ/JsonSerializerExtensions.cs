using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Enhanced JsonSerializer static methods with improved error handling and context information.
    /// Provides System.Text.Json-style API with JsonToolkit.STJ enhancements.
    /// </summary>
    public static class JsonSerializerExtensions
    {
        /// <summary>
        /// Serializes the specified value to JSON with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <returns>A JSON string representation of the value.</returns>
        /// <exception cref="JsonToolkitException">Thrown when serialization fails with enhanced context.</exception>
        public static string SerializeEnhanced<T>(T value, JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.Serialize(value, options);
            }
            catch (JsonException ex)
            {
                throw new JsonToolkitException(
                    $"Failed to serialize object of type '{typeof(T).Name}' to JSON.",
                    ex,
                    operation: "SerializeEnhanced"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Serialization of type '{typeof(T).Name}' is not supported with the current configuration.",
                    ex,
                    operation: "SerializeEnhanced"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while serializing object of type '{typeof(T).Name}'.",
                    ex,
                    operation: "SerializeEnhanced"
                );
            }
        }

        /// <summary>
        /// Serializes the specified value to JSON bytes with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <returns>A UTF-8 byte array representation of the JSON.</returns>
        /// <exception cref="JsonToolkitException">Thrown when serialization fails with enhanced context.</exception>
        public static byte[] SerializeEnhancedToUtf8Bytes<T>(T value, JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(value, options);
            }
            catch (JsonException ex)
            {
                throw new JsonToolkitException(
                    $"Failed to serialize object of type '{typeof(T).Name}' to UTF-8 bytes.",
                    ex,
                    operation: "SerializeEnhancedToUtf8Bytes"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Serialization of type '{typeof(T).Name}' to UTF-8 bytes is not supported with the current configuration.",
                    ex,
                    operation: "SerializeEnhancedToUtf8Bytes"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while serializing object of type '{typeof(T).Name}' to UTF-8 bytes.",
                    ex,
                    operation: "SerializeEnhancedToUtf8Bytes"
                );
            }
        }

        /// <summary>
        /// Deserializes JSON text to the specified type with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="json">The JSON text to deserialize.</param>
        /// <param name="options">Options to control deserialization behavior.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="JsonToolkitException">Thrown when deserialization fails with enhanced context.</exception>
        public static T DeserializeEnhanced<T>(string json, JsonSerializerOptions? options = null)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            try
            {
                var result = JsonSerializer.Deserialize<T>(json, options);
                return result!;
            }
            catch (JsonException ex)
            {
                var propertyPath = ExtractPropertyPath(ex);
                var lineInfo = ExtractLineInfo(ex);
                
                throw new JsonToolkitException(
                    $"Failed to deserialize JSON to type '{typeof(T).Name}'. {lineInfo}",
                    ex,
                    propertyPath: propertyPath,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Deserialization to type '{typeof(T).Name}' is not supported with the current configuration.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (ArgumentException ex)
            {
                throw new JsonToolkitException(
                    $"Invalid JSON format for deserialization to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while deserializing JSON to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
        }

        /// <summary>
        /// Deserializes UTF-8 JSON bytes to the specified type with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="utf8Json">The UTF-8 JSON bytes to deserialize.</param>
        /// <param name="options">Options to control deserialization behavior.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="JsonToolkitException">Thrown when deserialization fails with enhanced context.</exception>
        public static T DeserializeEnhanced<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(utf8Json, options);
                return result!;
            }
            catch (JsonException ex)
            {
                var propertyPath = ExtractPropertyPath(ex);
                var lineInfo = ExtractLineInfo(ex);
                
                throw new JsonToolkitException(
                    $"Failed to deserialize UTF-8 JSON bytes to type '{typeof(T).Name}'. {lineInfo}",
                    ex,
                    propertyPath: propertyPath,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Deserialization of UTF-8 JSON bytes to type '{typeof(T).Name}' is not supported with the current configuration.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (ArgumentException ex)
            {
                throw new JsonToolkitException(
                    $"Invalid UTF-8 JSON format for deserialization to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while deserializing UTF-8 JSON bytes to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhanced"
                );
            }
        }

        /// <summary>
        /// Deserializes UTF-8 JSON bytes to the specified type with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="utf8Json">The UTF-8 JSON bytes to deserialize.</param>
        /// <param name="options">Options to control deserialization behavior.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="JsonToolkitException">Thrown when deserialization fails with enhanced context.</exception>
        public static T DeserializeEnhanced<T>(byte[] utf8Json, JsonSerializerOptions? options = null)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            return DeserializeEnhanced<T>(utf8Json.AsSpan(), options);
        }

        /// <summary>
        /// Asynchronously serializes the specified value to a stream with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="stream">The stream to write the JSON to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="JsonToolkitException">Thrown when serialization fails with enhanced context.</exception>
        public static async Task SerializeEnhancedAsync<T>(Stream stream, T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new JsonToolkitException(
                    $"Failed to serialize object of type '{typeof(T).Name}' to stream.",
                    ex,
                    operation: "SerializeEnhancedAsync"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Serialization of type '{typeof(T).Name}' to stream is not supported with the current configuration.",
                    ex,
                    operation: "SerializeEnhancedAsync"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while serializing object of type '{typeof(T).Name}' to stream.",
                    ex,
                    operation: "SerializeEnhancedAsync"
                );
            }
        }

        /// <summary>
        /// Asynchronously deserializes JSON from a stream to the specified type with enhanced error handling.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the JSON to deserialize.</param>
        /// <param name="options">Options to control deserialization behavior.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the deserialized object.</returns>
        /// <exception cref="JsonToolkitException">Thrown when deserialization fails with enhanced context.</exception>
        public static async Task<T> DeserializeEnhancedAsync<T>(Stream stream, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                var result = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
                return result!;
            }
            catch (JsonException ex)
            {
                var propertyPath = ExtractPropertyPath(ex);
                var lineInfo = ExtractLineInfo(ex);
                
                throw new JsonToolkitException(
                    $"Failed to deserialize JSON from stream to type '{typeof(T).Name}'. {lineInfo}",
                    ex,
                    propertyPath: propertyPath,
                    operation: "DeserializeEnhancedAsync"
                );
            }
            catch (NotSupportedException ex)
            {
                throw new JsonToolkitException(
                    $"Deserialization from stream to type '{typeof(T).Name}' is not supported with the current configuration.",
                    ex,
                    operation: "DeserializeEnhancedAsync"
                );
            }
            catch (ArgumentException ex)
            {
                throw new JsonToolkitException(
                    $"Invalid JSON format in stream for deserialization to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhancedAsync"
                );
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Unexpected error occurred while deserializing JSON from stream to type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeserializeEnhancedAsync"
                );
            }
        }

        /// <summary>
        /// Extracts property path information from a JsonException.
        /// </summary>
        /// <param name="ex">The JsonException to extract information from.</param>
        /// <returns>The property path if available, otherwise null.</returns>
        private static string? ExtractPropertyPath(JsonException ex)
        {
            // Try to extract property path from the exception message
            // System.Text.Json includes path information in the message
            var message = ex.Message;
            
            // Look for patterns like "Path: $.property.nested" or similar
            if (message.Contains("Path:"))
            {
                var pathIndex = message.IndexOf("Path:");
                if (pathIndex >= 0)
                {
                    var pathStart = pathIndex + 5; // "Path:".Length
                    var pathEnd = message.IndexOf('|', pathStart);
                    if (pathEnd == -1) pathEnd = message.IndexOf('.', pathStart);
                    if (pathEnd == -1) pathEnd = message.Length;
                    
                    var path = message.Substring(pathStart, pathEnd - pathStart).Trim();
                    return string.IsNullOrEmpty(path) ? null : path;
                }
            }

            // Try to extract from Path property if available (newer versions of System.Text.Json)
            try
            {
                var pathProperty = ex.GetType().GetProperty("Path");
                if (pathProperty != null)
                {
                    var pathValue = pathProperty.GetValue(ex) as string;
                    return string.IsNullOrEmpty(pathValue) ? null : pathValue;
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return null;
        }

        /// <summary>
        /// Extracts line and column information from a JsonException.
        /// </summary>
        /// <param name="ex">The JsonException to extract information from.</param>
        /// <returns>A formatted string with line/column information if available.</returns>
        private static string ExtractLineInfo(JsonException ex)
        {
            try
            {
                // Try to extract LineNumber and BytePositionInLine properties
                var lineNumberProperty = ex.GetType().GetProperty("LineNumber");
                var bytePositionProperty = ex.GetType().GetProperty("BytePositionInLine");

                if (lineNumberProperty != null && bytePositionProperty != null)
                {
                    var lineNumber = lineNumberProperty.GetValue(ex);
                    var bytePosition = bytePositionProperty.GetValue(ex);

                    if (lineNumber != null && bytePosition != null)
                    {
                        return $"Line: {lineNumber}, Position: {bytePosition}.";
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return string.Empty;
        }
    }
}