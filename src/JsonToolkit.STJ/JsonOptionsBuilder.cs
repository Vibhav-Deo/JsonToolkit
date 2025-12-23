using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Fluent builder for configuring JsonSerializerOptions with JsonToolkit.STJ enhancements.
    /// </summary>
    public class JsonOptionsBuilder
    {
        private readonly JsonSerializerOptions _options;
        private readonly List<JsonConverter> _converters;
        private bool _caseInsensitiveProperties;
        private bool _flexibleEnums;
        private readonly Dictionary<Type, object> _optionalDefaults;
        private readonly List<Action<JsonSerializerOptions>> _configurations;

        /// <summary>
        /// Initializes a new instance of the JsonOptionsBuilder class.
        /// </summary>
        public JsonOptionsBuilder()
        {
            _options = new JsonSerializerOptions();
            _converters = new List<JsonConverter>();
            _optionalDefaults = new Dictionary<Type, object>();
            _configurations = new List<Action<JsonSerializerOptions>>();
        }

        /// <summary>
        /// Initializes a new instance of the JsonOptionsBuilder class with base options.
        /// </summary>
        /// <param name="baseOptions">Base JsonSerializerOptions to start with.</param>
        public JsonOptionsBuilder(JsonSerializerOptions baseOptions)
        {
            _options = new JsonSerializerOptions(baseOptions);
            _converters = new List<JsonConverter>();
            _optionalDefaults = new Dictionary<Type, object>();
            _configurations = new List<Action<JsonSerializerOptions>>();
        }

        /// <summary>
        /// Enables case-insensitive property matching.
        /// </summary>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithCaseInsensitiveProperties()
        {
            _caseInsensitiveProperties = true;
            _configurations.Add(options => options.PropertyNameCaseInsensitive = true);
            return this;
        }

        /// <summary>
        /// Enables enhanced case-insensitive property matching with ambiguity detection.
        /// </summary>
        /// <param name="configure">Optional action to configure case-insensitive options.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithEnhancedCaseInsensitiveProperties(Action<CaseInsensitivePropertyOptions>? configure = null)
        {
            _caseInsensitiveProperties = true;
            _configurations.Add(options => options.WithEnhancedCaseInsensitiveProperties(configure));
            return this;
        }

        /// <summary>
        /// Enables strict case-sensitive property matching (opt-in strict mode).
        /// </summary>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithStrictCaseSensitiveProperties()
        {
            _caseInsensitiveProperties = false;
            _configurations.Add(options => options.WithStrictCaseSensitiveProperties());
            return this;
        }

        /// <summary>
        /// Enables flexible enum serialization (string/numeric with case-insensitive matching).
        /// </summary>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithFlexibleEnums()
        {
            _flexibleEnums = true;
            _configurations.Add(options => 
            {
                // Add a factory converter that creates FlexibleEnumConverter instances for enum types
                options.Converters.Add(new FlexibleEnumConverterFactory());
            });
            return this;
        }

        /// <summary>
        /// Enables flexible enum serialization with custom options.
        /// </summary>
        /// <param name="configure">Action to configure flexible enum options.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithFlexibleEnums(Action<FlexibleEnumOptions> configure)
        {
            _flexibleEnums = true;
            var options = new FlexibleEnumOptions();
            configure(options);
            
            _configurations.Add(serializerOptions => 
            {
                serializerOptions.Converters.Add(new FlexibleEnumConverterFactory(options));
            });
            return this;
        }

        /// <summary>
        /// Configures optional property defaults for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to configure defaults for.</typeparam>
        /// <param name="defaults">The default values to use.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithOptionalDefaults<T>(T defaults) where T : notnull
        {
            _optionalDefaults[typeof(T)] = defaults;
            return this;
        }

        /// <summary>
        /// Configures polymorphic type handling.
        /// </summary>
        /// <param name="configure">Action to configure polymorphic types.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithPolymorphicTypes(Action<PolymorphicTypeBuilder> configure)
        {
            var builder = new PolymorphicTypeBuilder();
            configure(builder);
            
            _configurations.Add(options =>
            {
                // This will be implemented when we create the PolymorphicConverter
                // For now, we'll store the configuration for later use
            });
            
            return this;
        }

        /// <summary>
        /// Adds a custom converter to the options.
        /// </summary>
        /// <param name="converter">The converter to add.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithConverter(JsonConverter converter)
        {
            _converters.Add(converter);
            return this;
        }

        /// <summary>
        /// Configures the property naming policy.
        /// </summary>
        /// <param name="namingPolicy">The naming policy to use.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithNamingPolicy(JsonNamingPolicy? namingPolicy)
        {
            _configurations.Add(options => options.PropertyNamingPolicy = namingPolicy);
            return this;
        }

        /// <summary>
        /// Configures whether to write indented JSON.
        /// </summary>
        /// <param name="writeIndented">True to write indented JSON, false otherwise.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithIndentation(bool writeIndented = true)
        {
            _configurations.Add(options => options.WriteIndented = writeIndented);
            return this;
        }

        /// <summary>
        /// Configures how to handle null values during serialization.
        /// </summary>
        /// <param name="ignoreNullValues">True to ignore null values, false to include them.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder WithNullHandling(bool ignoreNullValues = true)
        {
            _configurations.Add(options => options.DefaultIgnoreCondition = 
                ignoreNullValues ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never);
            return this;
        }

        /// <summary>
        /// Applies a custom configuration action to the options.
        /// </summary>
        /// <param name="configure">The configuration action to apply.</param>
        /// <returns>The current JsonOptionsBuilder instance for method chaining.</returns>
        public JsonOptionsBuilder Configure(Action<JsonSerializerOptions> configure)
        {
            _configurations.Add(configure);
            return this;
        }

        /// <summary>
        /// Builds the configured JsonSerializerOptions.
        /// </summary>
        /// <returns>The configured JsonSerializerOptions instance.</returns>
        public JsonSerializerOptions Build()
        {
            try
            {
                // Apply all configurations
                foreach (var configuration in _configurations)
                {
                    configuration(_options);
                }

                // Add all converters
                foreach (var converter in _converters)
                {
                    _options.Converters.Add(converter);
                }

                // Validate the configuration
                ValidateConfiguration();

                return _options;
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    "Failed to build JsonSerializerOptions configuration.",
                    ex,
                    operation: "BuildConfiguration"
                );
            }
        }

        /// <summary>
        /// Validates the current configuration for consistency and correctness.
        /// </summary>
        private void ValidateConfiguration()
        {
            // Check for conflicting converter registrations
            var converterTypes = new HashSet<Type>();
            foreach (var converter in _options.Converters)
            {
                var converterType = converter.GetType();
                if (converterType.IsGenericType)
                {
                    var genericArgs = converterType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        var targetType = genericArgs[0];
                        if (!converterTypes.Add(targetType))
                        {
                            throw new JsonToolkitException(
                                $"Multiple converters registered for type '{targetType.Name}'. This may cause unpredictable behavior.",
                                operation: "ValidateConfiguration"
                            );
                        }
                    }
                }
            }

            // Validate that case-insensitive properties don't conflict with custom naming policies
            if (_caseInsensitiveProperties && _options.PropertyNamingPolicy != null)
            {
                // This is actually fine - both can work together
                // Just log a warning in a real implementation
            }
        }
    }

    /// <summary>
    /// Builder for configuring polymorphic type handling.
    /// </summary>
    public class PolymorphicTypeBuilder
    {
        private readonly Dictionary<string, Type> _typeMappings;
        private string _typeProperty;
        private Type? _baseType;
        private Type? _fallbackType;

        /// <summary>
        /// Initializes a new instance of the PolymorphicTypeBuilder class.
        /// </summary>
        public PolymorphicTypeBuilder()
        {
            _typeMappings = new Dictionary<string, Type>();
            _typeProperty = "$type";
        }

        /// <summary>
        /// Sets the property name used for type discrimination.
        /// </summary>
        /// <param name="propertyName">The property name to use.</param>
        /// <returns>The current PolymorphicTypeBuilder instance for method chaining.</returns>
        public PolymorphicTypeBuilder WithTypeProperty(string propertyName)
        {
            _typeProperty = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            return this;
        }

        /// <summary>
        /// Sets the base type for polymorphic deserialization.
        /// </summary>
        /// <typeparam name="T">The base type.</typeparam>
        /// <returns>The current PolymorphicTypeBuilder instance for method chaining.</returns>
        public PolymorphicTypeBuilder WithBaseType<T>()
        {
            _baseType = typeof(T);
            return this;
        }

        /// <summary>
        /// Sets the fallback type to use when type discrimination fails.
        /// </summary>
        /// <typeparam name="T">The fallback type.</typeparam>
        /// <returns>The current PolymorphicTypeBuilder instance for method chaining.</returns>
        public PolymorphicTypeBuilder WithFallbackType<T>()
        {
            _fallbackType = typeof(T);
            return this;
        }

        /// <summary>
        /// Maps a type discriminator value to a concrete type.
        /// </summary>
        /// <typeparam name="T">The concrete type.</typeparam>
        /// <param name="discriminator">The discriminator value.</param>
        /// <returns>The current PolymorphicTypeBuilder instance for method chaining.</returns>
        public PolymorphicTypeBuilder MapType<T>(string discriminator)
        {
            _typeMappings[discriminator] = typeof(T);
            return this;
        }

        /// <summary>
        /// Gets the configured type mappings.
        /// </summary>
        internal Dictionary<string, Type> TypeMappings => _typeMappings;

        /// <summary>
        /// Gets the configured type property name.
        /// </summary>
        internal string TypeProperty => _typeProperty;

        /// <summary>
        /// Gets the configured base type.
        /// </summary>
        internal Type? BaseType => _baseType;

        /// <summary>
        /// Gets the configured fallback type.
        /// </summary>
        internal Type? FallbackType => _fallbackType;
    }
}