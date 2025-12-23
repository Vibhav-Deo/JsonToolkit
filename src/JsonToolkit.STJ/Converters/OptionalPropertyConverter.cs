using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Converter that applies default values for missing optional properties during deserialization.
    /// Handles null value precedence and recursive default application.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public class OptionalPropertyConverter<T> : ReadOnlyJsonConverter<T> where T : class, new()
    {
        private readonly OptionalPropertyDefaults<T> _defaults;
        private readonly Dictionary<string, PropertyInfo> _propertyMap;

        /// <summary>
        /// Gets the converter name for debugging purposes.
        /// </summary>
        public override string ConverterName => $"OptionalPropertyConverter<{typeof(T).Name}>";

        /// <summary>
        /// Gets the converter precedence for ordering when multiple converters apply.
        /// </summary>
        public override int Precedence => 10; // Higher precedence than basic converters

        /// <summary>
        /// Initializes a new instance of the OptionalPropertyConverter class.
        /// </summary>
        /// <param name="defaults">The default values configuration.</param>
        public OptionalPropertyConverter(OptionalPropertyDefaults<T> defaults)
        {
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            _propertyMap = BuildPropertyMap();
        }

        /// <summary>
        /// Reads and converts the JSON to the specified type with optional property defaults applied.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted value with defaults applied.</returns>
        protected override T ReadValue(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                // Read the entire JSON into a JsonDocument first
                using var document = JsonDocument.ParseValue(ref reader);
                var jsonElement = document.RootElement;
                
                // Track which properties are present in the JSON and their null status
                var presentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var nullProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        presentProperties.Add(property.Name);
                        if (property.Value.ValueKind == JsonValueKind.Null)
                        {
                            nullProperties.Add(property.Name);
                        }
                    }
                }
                
                // Create new options without any OptionalPropertyConverter to avoid recursion
                var tempOptions = new JsonSerializerOptions(options);
                
                // Remove all OptionalPropertyConverter instances and factories to prevent recursion
                var convertersToRemove = tempOptions.Converters
                    .Where(c => c is OptionalPropertyConverter<T> || c is OptionalPropertyConverterFactory)
                    .ToList();
                
                foreach (var converter in convertersToRemove)
                {
                    tempOptions.Converters.Remove(converter);
                }

                // Deserialize normally first - use JsonElement directly to avoid control character issues
                T result;
                try
                {
                    result = JsonSerializer.Deserialize<T>(jsonElement, tempOptions) ?? new T();
                }
                catch (JsonException)
                {
                    // Fallback: create new instance if deserialization fails due to control characters
                    result = new T();
                }
                
                // Apply global defaults for properties that were NOT present in JSON
                if (_defaults.DefaultValue != null)
                {
                    ApplyDefaultsForMissingProperties(result, _defaults.DefaultValue, presentProperties);
                }

                // Apply property-specific defaults for properties that were NOT present in JSON
                // OR were present but null and we're configured to ignore nulls
                foreach (var kvp in _defaults.PropertyDefaults)
                {
                    bool shouldApplyDefault = !presentProperties.Contains(kvp.Key) || 
                                            (nullProperties.Contains(kvp.Key) && _defaults.IgnoreNullValues);
                    
                    if (shouldApplyDefault)
                    {
                        ApplyDefaultIfMissing(result, kvp.Key, kvp.Value);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Failed to deserialize type '{typeof(T).Name}' with optional property defaults.",
                    ex,
                    operation: "ReadValue"
                );
            }
        }

        /// <summary>
        /// Applies defaults only for properties that were not present in the JSON.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="defaultSource">The source object containing default values.</param>
        /// <param name="presentProperties">Set of property names that were present in the JSON.</param>
        private void ApplyDefaultsForMissingProperties(T target, T defaultSource, HashSet<string> presentProperties)
        {
            foreach (var property in _propertyMap.Values)
            {
                if (property.CanWrite && property.CanRead && !presentProperties.Contains(property.Name))
                {
                    var defaultValue = property.GetValue(defaultSource);
                    if (defaultValue != null)
                    {
                        property.SetValue(target, defaultValue);
                    }
                }
                else if (property.CanRead && _defaults.ApplyRecursively && presentProperties.Contains(property.Name))
                {
                    // Apply recursive defaults to nested objects that were present but may have missing properties
                    var currentValue = property.GetValue(target);
                    var defaultValue = property.GetValue(defaultSource);
                    
                    if (currentValue != null && defaultValue != null && 
                        property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    {
                        ApplyRecursiveDefaults(currentValue, defaultValue, property.PropertyType);
                    }
                }
            }
        }

        /// <summary>
        /// Applies a default value for a specific property if it's still at its default value.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value to apply.</param>
        private void ApplyDefaultIfMissing(T target, string propertyName, object? defaultValue)
        {
            if (_propertyMap.TryGetValue(propertyName, out var property) && property.CanWrite && property.CanRead)
            {
                var currentValue = property.GetValue(target);
                
                // Only apply default if current value is the type's default value
                if (IsDefaultValue(currentValue, property.PropertyType) && defaultValue != null)
                {
                    try
                    {
                        var convertedValue = ConvertValue(defaultValue, property.PropertyType);
                        property.SetValue(target, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonToolkitException(
                            $"Failed to set default value for property '{propertyName}' on type '{typeof(T).Name}'.",
                            ex,
                            propertyPath: propertyName,
                            operation: "ApplyDefault"
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a value is the default value for its type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="type">The type.</param>
        /// <returns>True if the value is the default value for the type.</returns>
        private static bool IsDefaultValue(object? value, Type type)
        {
            if (value == null)
                return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
            
            if (type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                return value.Equals(defaultValue);
            }
            
            return false;
        }



        /// <summary>
        /// Converts a value to the specified type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The converted value.</returns>
        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    return ConvertValue(value, underlyingType);
                }
            }

            // Sanitize strings that might contain control characters
            if (targetType == typeof(string) && value is string stringValue)
            {
                return SanitizeString(stringValue);
            }

            // Use Convert.ChangeType for basic type conversions
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                // If conversion fails, return the original value and let the property setter handle it
                return value;
            }
        }

        /// <summary>
        /// Applies recursive defaults to nested objects.
        /// </summary>
        /// <param name="target">The target nested object.</param>
        /// <param name="defaultSource">The default source nested object.</param>
        /// <param name="objectType">The type of the nested object.</param>
        private void ApplyRecursiveDefaults(object target, object defaultSource, Type objectType)
        {
            try
            {
                var properties = objectType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                foreach (var property in properties)
                {
                    if (property.CanWrite && property.CanRead)
                    {
                        var currentValue = property.GetValue(target);
                        var defaultValue = property.GetValue(defaultSource);
                        
                        // If current value is null or default, apply the default
                        if (IsDefaultValue(currentValue, property.PropertyType) && defaultValue != null)
                        {
                            try
                            {
                                var convertedValue = ConvertValue(defaultValue, property.PropertyType);
                                property.SetValue(target, convertedValue);
                            }
                            catch
                            {
                                // Ignore conversion errors for recursive defaults
                            }
                        }
                        // Recursively apply to nested objects
                        else if (currentValue != null && defaultValue != null && 
                                property.PropertyType.IsClass && property.PropertyType != typeof(string))
                        {
                            ApplyRecursiveDefaults(currentValue, defaultValue, property.PropertyType);
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in recursive application to prevent breaking the main functionality
            }
        }

        /// <summary>
        /// Sanitizes a string by removing or escaping control characters that might cause JSON serialization issues.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The sanitized string.</returns>
        private static string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Check if the string contains any control characters
            bool hasControlChars = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    hasControlChars = true;
                    break;
                }
            }

            if (!hasControlChars)
                return input;

            // Replace control characters with safe alternatives
            var result = new System.Text.StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                {
                    // Replace with Unicode escape sequence or remove
                    result.Append($"\\u{(int)c:X4}");
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Builds a map of property names to PropertyInfo objects.
        /// </summary>
        /// <returns>A dictionary mapping property names to PropertyInfo objects.</returns>
        private static Dictionary<string, PropertyInfo> BuildPropertyMap()
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                map[property.Name] = property;
            }

            return map;
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the converter can convert the type; otherwise, false.</returns>
        protected override bool CanConvertType(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }
    }
}