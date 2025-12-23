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
                var context = ErrorContext.FromReader(ref reader, "Read")
                    .WithContext("ConverterName", ConverterName)
                    .WithContext("TargetType", typeToConvert.Name);

                var message = context.GetFormattedMessage($"Error reading value of type '{typeToConvert.Name}' using converter '{ConverterName}'");
                
                throw new JsonToolkitException(
                    message,
                    ex,
                    propertyPath: context.PropertyPath,
                    operation: context.Operation
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
                var context = ErrorContext.ForConverter(ConverterName, operation: "Write")
                    .WithContext("TargetType", typeof(T).Name)
                    .WithContext("ValueType", value?.GetType().Name ?? "null");

                var message = context.GetFormattedMessage($"Error writing value of type '{typeof(T).Name}' using converter '{ConverterName}'");
                
                throw new JsonToolkitException(
                    message,
                    ex,
                    operation: context.Operation
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
            return $"Converter: {ConverterName}, Type: {typeof(T).Name}, Precedence: {Precedence}, Capabilities: Read/Write";
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

    }
}