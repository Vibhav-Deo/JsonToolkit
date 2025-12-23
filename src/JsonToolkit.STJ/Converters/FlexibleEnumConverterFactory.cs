namespace JsonToolkit.STJ.Converters;
    /// <summary>
    /// Factory for creating FlexibleEnumConverter instances for enum types.
    /// </summary>
    public class FlexibleEnumConverterFactory : JsonConverterFactory
    {
        private readonly FlexibleEnumOptions _defaultOptions;

        /// <summary>
        /// Initializes a new instance of the FlexibleEnumConverterFactory class with default options.
        /// </summary>
        public FlexibleEnumConverterFactory()
        {
            _defaultOptions = new FlexibleEnumOptions();
        }

        /// <summary>
        /// Initializes a new instance of the FlexibleEnumConverterFactory class with specified options.
        /// </summary>
        /// <param name="defaultOptions">The default options to use for all enum converters.</param>
        public FlexibleEnumConverterFactory(FlexibleEnumOptions defaultOptions)
        {
            _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
        }

        /// <summary>
        /// Determines whether the converter factory can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the type is an enum or nullable enum; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            // Handle direct enum types
            if (typeToConvert.IsEnum)
            {
                return true;
            }

            // Handle nullable enum types
            if (typeToConvert.IsGenericType && 
                typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                return underlyingType?.IsEnum == true;
            }

            return false;
        }

        /// <summary>
        /// Creates a converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to create a converter for.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A JsonConverter instance for the specified type.</returns>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type enumType;
            bool isNullable = false;

            // Determine the actual enum type
            if (typeToConvert.IsEnum)
            {
                enumType = typeToConvert;
            }
            else if (typeToConvert.IsGenericType && 
                     typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                enumType = Nullable.GetUnderlyingType(typeToConvert)!;
                isNullable = true;
            }
            else
            {
                throw new JsonToolkitException(
                    $"Cannot create FlexibleEnumConverter for non-enum type: {typeToConvert.Name}",
                    operation: "CreateConverter"
                );
            }

            // Create the appropriate converter type
            Type converterType;
            object[] constructorArgs;
            
            if (isNullable)
            {
                converterType = typeof(NullableFlexibleEnumConverter<>).MakeGenericType(enumType);
                constructorArgs = [_defaultOptions];
            }
            else
            {
                converterType = typeof(FlexibleEnumConverter<>).MakeGenericType(enumType);
                constructorArgs = [_defaultOptions];
            }

            // Create converter instance with options
            return (JsonConverter)Activator.CreateInstance(converterType, constructorArgs)!;
        }
    }

    /// <summary>
    /// A flexible enum converter for nullable enum types.
    /// </summary>
    /// <typeparam name="T">The enum type to convert.</typeparam>
    public class NullableFlexibleEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
    {
        private readonly FlexibleEnumConverter<T> _innerConverter;

        /// <summary>
        /// Initializes a new instance of the NullableFlexibleEnumConverter class.
        /// </summary>
        /// <param name="options">The options for enum conversion.</param>
        public NullableFlexibleEnumConverter(FlexibleEnumOptions options)
        {
            _innerConverter = new FlexibleEnumConverter<T>(options);
        }

        /// <summary>
        /// Reads and converts the JSON to the specified nullable enum type.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted nullable enum value.</returns>
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _innerConverter.Read(ref reader, typeof(T), options);
        }

        /// <summary>
        /// Writes the specified nullable enum value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The nullable enum value to convert.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                _innerConverter.Write(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        /// <summary>
        /// Determines whether this converter can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the converter can convert the type; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T?) ||
                   (typeToConvert.IsGenericType &&
                    typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    typeToConvert.GetGenericArguments()[0] == typeof(T));
        }
    }
