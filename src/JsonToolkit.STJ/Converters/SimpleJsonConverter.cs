using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Base class for creating simple JSON converters with reduced boilerplate.
    /// Provides error wrapping and context information for converter operations.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public abstract class SimpleJsonConverter<T> : JsonConverter<T>
    {
        /// <summary>
        /// Gets the converter name for debugging purposes.
        /// </summary>
        public virtual string ConverterName => GetType().Name;

        /// <summary>
        /// Gets the converter precedence for ordering when multiple converters apply.
        /// Higher values have higher precedence.
        /// </summary>
        public virtual int Precedence => 0;

        /// <summary>
        /// Reads and converts the JSON to the specified type.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted value.</returns>
        public sealed override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return ReadValue(ref reader, typeToConvert, options);
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    $"Error reading value of type '{typeToConvert.Name}' using converter '{ConverterName}'.",
                    ex,
                    propertyPath: GetReaderPath(ref reader),
                    operation: "Read"
                );
            }
        }

        /// <summary>
        /// Writes the specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The serializer options.</param>
        public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            try
            {
                WriteValue(writer, value, options);
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    $"Error writing value of type '{typeof(T).Name}' using converter '{ConverterName}'.",
                    ex,
                    operation: "Write"
                );
            }
        }

        /// <summary>
        /// Reads and converts the JSON to the specified type.
        /// Override this method to implement custom reading logic.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted value.</returns>
        protected abstract T ReadValue(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        /// <summary>
        /// Writes the specified value as JSON.
        /// Override this method to implement custom writing logic.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The serializer options.</param>
        protected abstract void WriteValue(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

        /// <summary>
        /// Gets debugging information about this converter.
        /// </summary>
        /// <returns>A string containing debugging information.</returns>
        public virtual string GetDebugInfo()
        {
            return $"Converter: {ConverterName}, Type: {typeof(T).Name}, Precedence: {Precedence}";
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the converter can convert the type; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert) && CanConvertType(typeToConvert);
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// Override this method to provide custom type checking logic.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the converter can convert the type; otherwise, false.</returns>
        protected virtual bool CanConvertType(Type typeToConvert)
        {
            return true;
        }

        /// <summary>
        /// Gets the current path from the reader, handling version compatibility.
        /// </summary>
        /// <param name="reader">The reader to get the path from.</param>
        /// <returns>The current path or null if not available.</returns>
        private static string? GetReaderPath(ref Utf8JsonReader reader)
        {
            // Path property is not consistently available across all target frameworks
            // For now, return null - this is acceptable as the path is used for enhanced error messages
            // In a future version, we could implement path tracking manually if needed
            return null;
        }
    }
}