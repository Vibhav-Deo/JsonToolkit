using System.Globalization;
using System.Linq;
using System.Reflection;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// A flexible enum converter that supports string/numeric serialization with case-insensitive matching,
    /// configurable fallback behavior, flags enum support, and custom naming through attributes.
    /// </summary>
    /// <typeparam name="T">The enum type to convert.</typeparam>
    public class FlexibleEnumConverter<T> : SimpleJsonConverter<T> where T : struct, Enum
    {
        private readonly FlexibleEnumOptions _options;
        private readonly Dictionary<string, T> _nameToValue;
        private readonly Dictionary<T, string> _valueToName;
        private readonly bool _isFlags;
        private readonly T? _fallbackValue;

        /// <summary>
        /// Initializes a new instance of the FlexibleEnumConverter class with default options.
        /// </summary>
        public FlexibleEnumConverter() : this(new FlexibleEnumOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the FlexibleEnumConverter class with specified options.
        /// </summary>
        /// <param name="options">The options for enum conversion.</param>
        public FlexibleEnumConverter(FlexibleEnumOptions options) : this(options, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FlexibleEnumConverter class with specified options.
        /// </summary>
        /// <param name="options">The options for enum conversion.</param>
        /// <param name="fallbackValue">Optional fallback value for this specific enum type.</param>
        public FlexibleEnumConverter(FlexibleEnumOptions options, T? fallbackValue)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _fallbackValue = fallbackValue;
            _isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;
            
            (_nameToValue, _valueToName) = BuildNameMappings();
        }

        /// <summary>
        /// Gets the converter name for debugging purposes.
        /// </summary>
        public override string ConverterName => $"FlexibleEnumConverter<{typeof(T).Name}>";

        /// <summary>
        /// Gets the converter precedence for ordering when multiple converters apply.
        /// </summary>
        public override int Precedence => 100; // Higher precedence than default converters

        /// <summary>
        /// Reads and converts the JSON to the specified enum type.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted enum value.</returns>
        protected override T ReadValue(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var stringValue = reader.GetString();
                    return ParseStringValue(stringValue ?? string.Empty);

                case JsonTokenType.Number:
                    if (_options.AllowNumericValues)
                    {
                        if (reader.TryGetInt64(out var longValue))
                        {
                            return ParseNumericValue(longValue);
                        }
                        if (reader.TryGetUInt64(out var ulongValue))
                        {
                            return ParseNumericValue((long)ulongValue);
                        }
                    }
                    return HandleUndefinedValue($"Numeric value not allowed or invalid: {reader.GetString()}");

                default:
                    return HandleUndefinedValue($"Unexpected token type: {reader.TokenType}");
            }
        }

        /// <summary>
        /// Writes the specified enum value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The enum value to convert.</param>
        /// <param name="options">The serializer options.</param>
        protected override void WriteValue(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_options.SerializeAsString)
            {
                var stringValue = GetStringRepresentation(value);
                writer.WriteStringValue(stringValue);
            }
            else
            {
                var numericValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                writer.WriteNumberValue(numericValue);
            }
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the converter can convert the type; otherwise, false.</returns>
        protected override bool CanConvertType(Type typeToConvert)
        {
            return typeToConvert == typeof(T) || 
                   (typeToConvert.IsGenericType && 
                    typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    typeToConvert.GetGenericArguments()[0] == typeof(T));
        }

        private (Dictionary<string, T>, Dictionary<T, string>) BuildNameMappings()
        {
            var nameToValue = new Dictionary<string, T>(_options.CaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            var valueToName = new Dictionary<T, string>();

            var enumType = typeof(T);
            var enumValues = Enum.GetValues(enumType).Cast<T>().ToArray();

            foreach (var enumValue in enumValues)
            {
                var fieldInfo = enumType.GetField(enumValue.ToString());
                if (fieldInfo == null) continue;

                // Check for custom naming attributes
                var customName = GetCustomEnumName(fieldInfo);
                var displayName = customName ?? enumValue.ToString();

                // Store the primary mapping
                valueToName[enumValue] = displayName;
                
                // Add all possible name variations for parsing
                AddNameVariation(nameToValue, displayName, enumValue);
                AddNameVariation(nameToValue, enumValue.ToString(), enumValue);
                
                if (customName != null)
                {
                    AddNameVariation(nameToValue, customName, enumValue);
                }
            }

            return (nameToValue, valueToName);
        }

        private void AddNameVariation(Dictionary<string, T> nameToValue, string name, T value)
        {
            if (string.IsNullOrEmpty(name)) return;

            var comparer = _options.CaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            
            // Check for conflicts only if case-sensitive
            if (!_options.CaseInsensitive && nameToValue.ContainsKey(name))
            {
                if (!nameToValue[name].Equals(value))
                {
                    throw new JsonToolkitException(
                        $"Enum name conflict: '{name}' maps to both '{nameToValue[name]}' and '{value}' in enum type '{typeof(T).Name}'.",
                        operation: "EnumMapping"
                    );
                }
            }
            else if (_options.CaseInsensitive)
            {
                // For case-insensitive, check if there's a conflict with different casing
                var existingKey = nameToValue.Keys.FirstOrDefault(k => comparer.Equals(k, name));
                if (existingKey != null && !nameToValue[existingKey].Equals(value))
                {
                    throw new JsonToolkitException(
                        $"Case-insensitive enum name conflict: '{name}' conflicts with existing '{existingKey}' in enum type '{typeof(T).Name}'.",
                        operation: "EnumMapping"
                    );
                }
            }

            nameToValue[name] = value;
        }

        private string? GetCustomEnumName(FieldInfo fieldInfo)
        {
            // Check for JsonPropertyNameAttribute first
            var jsonPropertyName = fieldInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonPropertyName != null)
            {
                return jsonPropertyName.Name;
            }

            // Check for other common naming attributes
            var displayAttribute = fieldInfo.GetCustomAttributes()
                .FirstOrDefault(attr => attr.GetType().Name == "DisplayAttribute");
            if (displayAttribute != null)
            {
                var nameProperty = displayAttribute.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    return nameProperty.GetValue(displayAttribute) as string;
                }
            }

            return null;
        }

        private T ParseStringValue(string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return HandleUndefinedValue("Empty string");
            }

            // Handle flags enum with comma-separated values
            if (_isFlags && stringValue.Contains(","))
            {
                return ParseFlagsValue(stringValue);
            }

            // Try direct lookup first
            if (_nameToValue.TryGetValue(stringValue, out var directMatch))
            {
                return directMatch;
            }

            // Try parsing as numeric string if allowed
            if (_options.AllowNumericValues && long.TryParse(stringValue, out var numericValue))
            {
                return ParseNumericValue(numericValue);
            }

            return HandleUndefinedValue($"Unknown enum value: '{stringValue}'");
        }

        private T ParseFlagsValue(string stringValue)
        {
            if (!_isFlags)
            {
                return HandleUndefinedValue($"Comma-separated values not supported for non-flags enum: '{stringValue}'");
            }

            var parts = stringValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            long combinedValue = 0;

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (_nameToValue.TryGetValue(trimmedPart, out var flagValue))
                {
                    combinedValue |= Convert.ToInt64(flagValue, CultureInfo.InvariantCulture);
                }
                else if (_options.AllowNumericValues && long.TryParse(trimmedPart, out var numericFlag))
                {
                    combinedValue |= numericFlag;
                }
                else
                {
                    return HandleUndefinedValue($"Unknown flag value: '{trimmedPart}' in '{stringValue}'");
                }
            }

            return (T)Enum.ToObject(typeof(T), combinedValue);
        }

        private T ParseNumericValue(long numericValue)
        {
            // Convert to the enum's underlying type for IsDefined check
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            object convertedValue;
            
            try
            {
                convertedValue = Convert.ChangeType(numericValue, underlyingType, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return HandleUndefinedValue($"Numeric value {numericValue} is out of range for enum type {typeof(T).Name}");
            }

            if (Enum.IsDefined(typeof(T), convertedValue))
            {
                return (T)Enum.ToObject(typeof(T), numericValue);
            }

            // For flags enums, allow undefined combinations
            if (_isFlags)
            {
                return (T)Enum.ToObject(typeof(T), numericValue);
            }

            return HandleUndefinedValue($"Undefined enum value: {numericValue}");
        }

        private string GetStringRepresentation(T value)
        {
            // Handle flags enum
            if (_isFlags)
            {
                return GetFlagsStringRepresentation(value);
            }

            // Use custom name if available
            if (_valueToName.TryGetValue(value, out var customName))
            {
                return customName;
            }

            // Fallback to default string representation
            return value.ToString();
        }

        private string GetFlagsStringRepresentation(T value)
        {
            var numericValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            
            // If it's a single defined value, use the name
            if (_valueToName.TryGetValue(value, out var singleName))
            {
                return singleName;
            }

            // For combined flags, build comma-separated string
            var flagNames = new List<string>();
            var remainingValue = numericValue;

            // Get all defined enum values in descending order (to handle combined flags properly)
            // Cache the numeric values to avoid repeated Convert.ToInt64 calls
            var definedValues = Enum.GetValues(typeof(T)).Cast<T>()
                .Select(v => new { Value = v, Numeric = Convert.ToInt64(v, CultureInfo.InvariantCulture) })
                .Where(v => v.Numeric != 0) // Skip zero values for flags
                .OrderByDescending(v => v.Numeric)
                .ToArray();

            foreach (var definedValue in definedValues)
            {
                if ((remainingValue & definedValue.Numeric) == definedValue.Numeric)
                {
                    flagNames.Add(_valueToName.TryGetValue(definedValue.Value, out var name) ? name : definedValue.Value.ToString());
                    remainingValue &= ~definedValue.Numeric;
                }
            }

            // If there are remaining bits, include them as numeric
            if (remainingValue != 0)
            {
                flagNames.Add(remainingValue.ToString());
            }

            // Reverse to get the original order
            flagNames.Reverse();
            
            return flagNames.Count > 0 ? string.Join(", ", flagNames) : "0";
        }

        private T HandleUndefinedValue(string errorMessage)
        {
            switch (_options.UndefinedValueHandling)
            {
                case UndefinedEnumValueHandling.ThrowException:
                    throw new JsonToolkitException(
                        $"Enum conversion failed for type '{typeof(T).Name}': {errorMessage}",
                        operation: "EnumConversion"
                    );

                case UndefinedEnumValueHandling.ReturnDefault:
                    return default(T);

                case UndefinedEnumValueHandling.ReturnFallbackValue:
                    if (_fallbackValue.HasValue)
                    {
                        return _fallbackValue.Value;
                    }
                    return default(T);

                default:
                    throw new JsonToolkitException(
                        $"Unknown UndefinedValueHandling option: {_options.UndefinedValueHandling}",
                        operation: "EnumConversion"
                    );
            }
        }
    }

    /// <summary>
    /// Options for configuring FlexibleEnumConverter behavior.
    /// </summary>
    public class FlexibleEnumOptions
    {
        /// <summary>
        /// Gets or sets whether to serialize enums as strings. Default is true.
        /// </summary>
        public bool SerializeAsString { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to allow numeric values during deserialization. Default is true.
        /// </summary>
        public bool AllowNumericValues { get; set; } = true;

        /// <summary>
        /// Gets or sets whether enum name matching should be case-insensitive. Default is true.
        /// </summary>
        public bool CaseInsensitive { get; set; } = true;

        /// <summary>
        /// Gets or sets how to handle undefined enum values. Default is ThrowException.
        /// </summary>
        public UndefinedEnumValueHandling UndefinedValueHandling { get; set; } = UndefinedEnumValueHandling.ThrowException;
    }

    /// <summary>
    /// Defines how to handle undefined enum values during deserialization.
    /// </summary>
    public enum UndefinedEnumValueHandling
    {
        /// <summary>
        /// Throw an exception when encountering undefined values.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Return the default value of the enum type.
        /// </summary>
        ReturnDefault,

        /// <summary>
        /// Return a specified fallback value.
        /// </summary>
        ReturnFallbackValue
    }
}