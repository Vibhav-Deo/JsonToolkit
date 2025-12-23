using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonToolkit.STJ.ValidationAttributes;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Factory for creating ValidationConverter instances for types that have validation attributes.
    /// </summary>
    public class ValidationConverterFactory : JsonConverterFactory
    {
        private readonly ValidationOptions _options;

        /// <summary>
        /// Initializes a new instance of the ValidationConverterFactory class.
        /// </summary>
        /// <param name="options">The validation options to use.</param>
        public ValidationConverterFactory(ValidationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the ValidationConverterFactory class with default options.
        /// </summary>
        public ValidationConverterFactory() : this(new ValidationOptions())
        {
        }

        /// <summary>
        /// Determines whether the converter instance can convert the specified object type.
        /// </summary>
        /// <param name="typeToConvert">The type of object to check.</param>
        /// <returns>True if the instance can convert the specified object type; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            // Only convert reference types (classes) that are not built-in types
            if (!typeToConvert.IsClass || typeToConvert == typeof(string) || typeToConvert.IsArray)
                return false;

            // Skip system types and primitive types
            if (typeToConvert.Namespace?.StartsWith("System") == true && !typeToConvert.IsGenericType)
                return false;

            // Check if the type has any properties with validation attributes
            return HasValidationAttributes(typeToConvert);
        }

        /// <summary>
        /// Creates a converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type handled by the converter.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>A converter for the specified type.</returns>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // Create ValidationConverter<T> using reflection
            try
            {
                var converterType = typeof(ValidationConverter<>).MakeGenericType(typeToConvert);
                
                return (JsonConverter)Activator.CreateInstance(
                    converterType,
                    _options
                )!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create ValidationConverter for type {typeToConvert.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a type has any properties with validation attributes.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type has validation attributes; otherwise, false.</returns>
        private static bool HasValidationAttributes(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var attributes = property.GetCustomAttributes()
                        .OfType<IJsonValidationAttribute>()
                        .ToArray();
                    if (attributes.Length > 0)
                        return true;
                }
            }

            return false;
        }
    }
}