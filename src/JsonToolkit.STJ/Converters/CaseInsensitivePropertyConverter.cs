using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Converter that provides enhanced case-insensitive property matching with ambiguity detection.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public class CaseInsensitivePropertyConverter<T> : SimpleJsonConverter<T> where T : class, new()
    {
        private readonly CaseInsensitivePropertyOptions _options;
        private readonly Dictionary<string, PropertyInfo> _propertyMap;
        private readonly HashSet<string> _ambiguousProperties;

        /// <summary>
        /// Initializes a new instance of the CaseInsensitivePropertyConverter class.
        /// </summary>
        /// <param name="options">Options for case-insensitive property matching.</param>
        public CaseInsensitivePropertyConverter(CaseInsensitivePropertyOptions? options = null)
        {
            _options = options ?? new CaseInsensitivePropertyOptions();
            _propertyMap = new Dictionary<string, PropertyInfo>();
            _ambiguousProperties = new HashSet<string>();
            
            BuildPropertyMap();
        }

        /// <summary>
        /// Gets the converter name for debugging purposes.
        /// </summary>
        public override string ConverterName => $"CaseInsensitivePropertyConverter<{typeof(T).Name}>";

        /// <summary>
        /// Gets the converter precedence for ordering when multiple converters apply.
        /// </summary>
        public override int Precedence => 10; // Higher precedence than default converters

        /// <summary>
        /// Reads and converts the JSON to the specified type.
        /// </summary>
        protected override T ReadValue(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
            }

            var instance = new T();
            var processedProperties = new HashSet<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
                }

                var propertyName = reader.GetString();
                if (string.IsNullOrEmpty(propertyName))
                {
                    reader.Skip();
                    continue;
                }

                // Find the matching property using case-insensitive logic
                var matchedProperty = FindMatchingProperty(propertyName, processedProperties);
                
                if (matchedProperty == null)
                {
                    // Skip unknown properties
                    reader.Read();
                    reader.Skip();
                    continue;
                }

                // Read the property value
                reader.Read();
                var propertyValue = JsonSerializer.Deserialize(ref reader, matchedProperty.PropertyType, options);
                
                // Set the property value
                if (matchedProperty.CanWrite)
                {
                    matchedProperty.SetValue(instance, propertyValue);
                    processedProperties.Add(GetPropertyKey(matchedProperty.Name));
                }
            }

            return instance;
        }

        /// <summary>
        /// Writes the specified value as JSON.
        /// </summary>
        protected override void WriteValue(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetCustomAttributes<JsonIgnoreAttribute>().Any());

            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(value);
                
                // Apply naming policy if available
                var propertyName = GetSerializedPropertyName(property, options);
                
                writer.WritePropertyName(propertyName);
                JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Builds the property map for case-insensitive matching.
        /// </summary>
        private void BuildPropertyMap()
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead || p.CanWrite);

            var propertyGroups = new Dictionary<string, List<PropertyInfo>>();

            // Group properties by their normalized names
            foreach (var property in properties)
            {
                var normalizedName = GetPropertyKey(property.Name);
                
                if (!propertyGroups.ContainsKey(normalizedName))
                {
                    propertyGroups[normalizedName] = new List<PropertyInfo>();
                }
                
                propertyGroups[normalizedName].Add(property);
            }

            // Build the property map and detect ambiguities
            foreach (var group in propertyGroups)
            {
                if (group.Value.Count > 1)
                {
                    // Multiple properties with the same normalized name - this is ambiguous
                    _ambiguousProperties.Add(group.Key);
                    
                    if (_options.StrictMode)
                    {
                        throw new JsonToolkitException(
                            $"Ambiguous property names detected for type '{typeof(T).Name}': " +
                            $"{string.Join(", ", group.Value.Select(p => p.Name))}. " +
                            "These properties differ only by case. Use strict mode to require exact case matching.",
                            operation: "BuildPropertyMap"
                        );
                    }
                    
                    // In non-strict mode, use the first property found
                    _propertyMap[group.Key] = group.Value.First();
                }
                else
                {
                    _propertyMap[group.Key] = group.Value.First();
                }
            }
        }

        /// <summary>
        /// Finds a matching property for the given JSON property name.
        /// </summary>
        private PropertyInfo? FindMatchingProperty(string jsonPropertyName, HashSet<string> processedProperties)
        {
            if (_options.StrictMode)
            {
                // In strict mode, require exact case matching
                var exactMatch = typeof(T).GetProperty(jsonPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (exactMatch != null)
                {
                    var exactKey = GetPropertyKey(exactMatch.Name);
                    if (!processedProperties.Contains(exactKey))
                    {
                        return exactMatch;
                    }
                }
                return null;
            }

            // Case-insensitive matching
            var normalizedName = GetPropertyKey(jsonPropertyName);
            
            if (_propertyMap.TryGetValue(normalizedName, out var property))
            {
                var propertyKey = GetPropertyKey(property.Name);
                
                // Check if this property has already been processed
                if (processedProperties.Contains(propertyKey))
                {
                    return null;
                }

                // Check for ambiguity
                if (_ambiguousProperties.Contains(normalizedName))
                {
                    if (_options.ThrowOnAmbiguity)
                    {
                        throw new JsonToolkitException(
                            $"Ambiguous property name '{jsonPropertyName}' for type '{typeof(T).Name}'. " +
                            "Multiple properties match this name when case is ignored. " +
                            "Use exact case matching or enable strict mode.",
                            propertyPath: jsonPropertyName,
                            operation: "FindMatchingProperty"
                        );
                    }
                }

                return property;
            }

            return null;
        }

        /// <summary>
        /// Gets the normalized property key for case-insensitive comparison.
        /// </summary>
        private string GetPropertyKey(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return string.Empty;

            switch (_options.ComparisonMode)
            {
                case CaseInsensitiveComparisonMode.InvariantCultureIgnoreCase:
                    return propertyName.ToUpperInvariant();
                    
                case CaseInsensitiveComparisonMode.CurrentCultureIgnoreCase:
                    return propertyName.ToUpper(CultureInfo.CurrentCulture);
                    
                case CaseInsensitiveComparisonMode.OrdinalIgnoreCase:
                default:
                    return propertyName.ToUpperInvariant();
            }
        }

        /// <summary>
        /// Gets the serialized property name, applying naming policies if available.
        /// </summary>
        private string GetSerializedPropertyName(PropertyInfo property, JsonSerializerOptions options)
        {
            // Check for JsonPropertyName attribute
            var jsonPropertyNameAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyNameAttr != null)
            {
                return jsonPropertyNameAttr.Name;
            }

            // Apply naming policy if available
            if (options.PropertyNamingPolicy != null)
            {
                return options.PropertyNamingPolicy.ConvertName(property.Name);
            }

            return property.Name;
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// </summary>
        protected override bool CanConvertType(Type typeToConvert)
        {
            return typeToConvert == typeof(T) && 
                   typeToConvert.IsClass && 
                   !typeToConvert.IsAbstract &&
                   typeToConvert.GetConstructor(Type.EmptyTypes) != null;
        }
    }

    /// <summary>
    /// Options for configuring case-insensitive property matching behavior.
    /// </summary>
    public class CaseInsensitivePropertyOptions
    {
        /// <summary>
        /// Gets or sets whether to use strict mode (case-sensitive matching).
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to throw an exception when ambiguous property names are encountered.
        /// </summary>
        public bool ThrowOnAmbiguity { get; set; } = false;

        /// <summary>
        /// Gets or sets the comparison mode for case-insensitive matching.
        /// </summary>
        public CaseInsensitiveComparisonMode ComparisonMode { get; set; } = CaseInsensitiveComparisonMode.OrdinalIgnoreCase;

        /// <summary>
        /// Gets or sets whether to handle special characters consistently across casing modes.
        /// </summary>
        public bool NormalizeSpecialCharacters { get; set; } = true;
    }

    /// <summary>
    /// Defines the comparison mode for case-insensitive property matching.
    /// </summary>
    public enum CaseInsensitiveComparisonMode
    {
        /// <summary>
        /// Use ordinal (binary) comparison, ignoring case.
        /// </summary>
        OrdinalIgnoreCase,

        /// <summary>
        /// Use invariant culture comparison, ignoring case.
        /// </summary>
        InvariantCultureIgnoreCase,

        /// <summary>
        /// Use current culture comparison, ignoring case.
        /// </summary>
        CurrentCultureIgnoreCase
    }
}