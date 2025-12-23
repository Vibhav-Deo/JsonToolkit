namespace JsonToolkit.STJ.Converters;
    /// <summary>
    /// Converter for polymorphic deserialization with type discriminator support.
    /// </summary>
    /// <typeparam name="TBase">The base type for polymorphic deserialization.</typeparam>
    public class PolymorphicConverter<TBase> : JsonConverter<TBase> where TBase : class
    {
        private readonly string _typeProperty;
        private readonly Dictionary<string, Type> _typeMappings;
        private readonly Type? _fallbackType;

        /// <summary>
        /// Initializes a new instance of the PolymorphicConverter class.
        /// </summary>
        /// <param name="typeProperty">The property name used for type discrimination.</param>
        /// <param name="typeMappings">Dictionary mapping discriminator values to concrete types.</param>
        /// <param name="fallbackType">Optional fallback type when discriminator is missing.</param>
        public PolymorphicConverter(string typeProperty, Dictionary<string, Type> typeMappings, Type? fallbackType = null)
        {
            _typeProperty = typeProperty ?? throw new ArgumentNullException(nameof(typeProperty));
            _typeMappings = typeMappings ?? throw new ArgumentNullException(nameof(typeMappings));
            _fallbackType = fallbackType;
        }

        public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonToolkitException(
                    $"Expected StartObject token, but got {reader.TokenType}",
                    operation: "PolymorphicDeserialization"
                );
            }

            // Read the entire object into a JsonDocument
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Try to find the type discriminator
            if (root.TryGetProperty(_typeProperty, out var typeElement))
            {
                var discriminator = typeElement.GetString();
                if (string.IsNullOrEmpty(discriminator))
                {
                    throw new JsonToolkitException(
                        $"Type discriminator property '{_typeProperty}' has null or empty value",
                        operation: "PolymorphicDeserialization"
                    );
                }

                if (!_typeMappings.TryGetValue(discriminator!, out var targetType))
                {
                    throw new JsonToolkitException(
                        $"Unknown type discriminator value: '{discriminator}'. Valid values are: {string.Join(", ", _typeMappings.Keys)}",
                        operation: "PolymorphicDeserialization"
                    );
                }

                // Create options without this converter to avoid infinite recursion
                var newOptions = new JsonSerializerOptions(options);
                newOptions.Converters.Clear();
                foreach (var converter in options.Converters)
                {
                    if (converter != this)
                        newOptions.Converters.Add(converter);
                }

                return (TBase?)JsonSerializer.Deserialize(root.GetRawText(), targetType, newOptions);
            }

            // No discriminator found - use fallback if configured
            if (_fallbackType != null)
            {
                // Create options without this converter to avoid infinite recursion
                var newOptions = new JsonSerializerOptions(options);
                newOptions.Converters.Clear();
                foreach (var converter in options.Converters)
                {
                    if (converter != this)
                        newOptions.Converters.Add(converter);
                }

                return (TBase?)JsonSerializer.Deserialize(root.GetRawText(), _fallbackType, newOptions);
            }

            throw new JsonToolkitException(
                $"Type discriminator property '{_typeProperty}' not found in JSON and no fallback type configured",
                operation: "PolymorphicDeserialization"
            );
        }

        public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var actualType = value.GetType();
            
            // Find the discriminator for this type
            string? discriminator = null;
            foreach (var kvp in _typeMappings)
            {
                if (kvp.Value == actualType)
                {
                    discriminator = kvp.Key;
                    break;
                }
            }

            if (discriminator == null)
            {
                throw new JsonToolkitException(
                    $"No type discriminator mapping found for type '{actualType.Name}'",
                    operation: "PolymorphicSerialization"
                );
            }

            // Serialize the object and inject the type discriminator
            var json = JsonSerializer.Serialize(value, actualType, options);
            using var doc = JsonDocument.Parse(json);
            
            writer.WriteStartObject();
            writer.WriteString(_typeProperty, discriminator);
            
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name != _typeProperty)
                {
                    property.WriteTo(writer);
                }
            }
            
            writer.WriteEndObject();
        }
    }
