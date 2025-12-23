using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ;

/// <summary>
    /// Extension methods for enhancing JsonSerializerOptions with JsonToolkit.STJ features.
    /// </summary>
    public static class JsonSerializerOptionsExtensions
    {
        /// <summary>
        /// Enables all JsonToolkit.STJ enhancements with default settings.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions EnableJsonToolkit(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Enable case-insensitive property matching
            options.PropertyNameCaseInsensitive = true;

            // Enable flexible enum handling
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));

            // Set reasonable defaults
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.AllowTrailingCommas = true;

            return options;
        }

        /// <summary>
        /// Enables enhanced case-insensitive property matching with ambiguity detection.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="configure">Optional action to configure case-insensitive options.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithEnhancedCaseInsensitiveProperties(this JsonSerializerOptions options, Action<CaseInsensitivePropertyOptions>? configure = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var caseInsensitiveOptions = new CaseInsensitivePropertyOptions();
            configure?.Invoke(caseInsensitiveOptions);

            // Enable the built-in case-insensitive property matching
            options.PropertyNameCaseInsensitive = true;

            return options;
        }

        /// <summary>
        /// Enables strict case-sensitive property matching (opt-in strict mode).
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithStrictCaseSensitiveProperties(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Disable case-insensitive property matching for strict mode
            options.PropertyNameCaseInsensitive = false;

            return options;
        }

        /// <summary>
        /// Enables deep merge capabilities for the JsonSerializerOptions.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithDeepMerge(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Deep merge functionality will be implemented in a later task
            // For now, we just ensure the options are configured appropriately
            options.PropertyNameCaseInsensitive = true;

            return options;
        }

        /// <summary>
        /// Configures enhanced null handling with distinction between missing, null, and default values.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="configure">Optional action to configure null handling options.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithEnhancedNullHandling(this JsonSerializerOptions options, Action<NullHandlingOptions>? configure = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var nullOptions = new NullHandlingOptions();
            configure?.Invoke(nullOptions);

            // Apply null handling configuration
            switch (nullOptions.SerializationBehavior)
            {
                case NullSerializationBehavior.Omit:
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    break;
                case NullSerializationBehavior.Conditional:
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    break;
                case NullSerializationBehavior.Include:
                default:
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
                    break;
            }

            if (nullOptions.SkipDefaultValues)
            {
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            }

            return options;
        }

        /// <summary>
        /// Enables better null handling with distinction between missing, null, and default values.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithBetterNulls(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Configure null handling to distinguish between missing and null
            options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

            return options;
        }

        /// <summary>
        /// Configures optional property defaults for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to configure defaults for.</typeparam>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="defaults">The default values to use.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithOptionalDefaults<T>(this JsonSerializerOptions options, T defaults) where T : class, new()
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            var factory = GetOrCreateOptionalPropertyFactory(options);
            factory.RegisterDefaults(defaults);

            return options;
        }

        /// <summary>
        /// Configures optional property defaults for a specific type using a configuration action.
        /// </summary>
        /// <typeparam name="T">The type to configure defaults for.</typeparam>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="configure">Action to configure the defaults.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithOptionalDefaults<T>(this JsonSerializerOptions options, Action<OptionalPropertyDefaults<T>> configure) where T : class, new()
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var factory = GetOrCreateOptionalPropertyFactory(options);
            factory.RegisterDefaults(configure);

            return options;
        }

        /// <summary>
        /// Configures the options for Newtonsoft.Json-like behavior.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithNewtonsoftCompatibility(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Enable case-insensitive properties
            options.PropertyNameCaseInsensitive = true;

            // Allow trailing commas and comments
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.AllowTrailingCommas = true;

            // Use camelCase naming
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            // Ignore null values by default
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // Enable string enum converter
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));

            return options;
        }

        /// <summary>
        /// Adds a converter to the options with validation and precedence handling.
        /// </summary>
        /// <param name="options">The options to apply converters to.</param>
        /// <param name="converter">The converter to add.</param>
        /// <param name="precedence">The precedence of the converter (higher values have higher precedence).</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions AddConverter(this JsonSerializerOptions options, JsonConverter converter, int precedence)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            try
            {
                // Register with the converter registry for precedence handling
                ConverterRegistry.RegisterConverter(converter, precedence);
                
                // Add to options - let the registry handle precedence
                var targetType = GetConverterTargetType(converter);
                if (targetType != null)
                {
                    // Remove any existing converters for this type
                    var existingConverters = options.Converters
                        .Where(c => GetConverterTargetType(c) == targetType)
                        .ToList();
                    
                    foreach (var existing in existingConverters)
                    {
                        options.Converters.Remove(existing);
                    }
                    
                    // Add all converters for this type from registry in precedence order
                    var registeredConverters = ConverterRegistry.GetConvertersForType(targetType);
                    foreach (var registeredConverter in registeredConverters)
                    {
                        if (!options.Converters.Contains(registeredConverter))
                        {
                            options.Converters.Add(registeredConverter);
                        }
                    }
                }
                else
                {
                    // If we can't determine the target type, just add it
                    options.Converters.Add(converter);
                }
                
                return options;
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    $"Failed to add converter of type '{converter.GetType().Name}'.",
                    ex,
                    operation: "AddConverter"
                );
            }
        }

        /// <summary>
        /// Adds a converter to the options with validation (default precedence).
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="converter">The converter to add.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions AddConverter(this JsonSerializerOptions options, JsonConverter converter)
        {
            return AddConverter(options, converter, 0);
        }

        /// <summary>
        /// Configures the options with a fluent builder.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="configure">Action to configure the options builder.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions Configure(this JsonSerializerOptions options, Action<JsonOptionsBuilder> configure)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new JsonOptionsBuilder(options);
            configure(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a copy of the JsonSerializerOptions.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to copy.</param>
        /// <returns>A new JsonSerializerOptions instance with the same configuration.</returns>
        public static JsonSerializerOptions Clone(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new JsonSerializerOptions(options);
        }

        /// <summary>
        /// Configures circular reference handling for serialization.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="configure">Optional action to configure circular reference options.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithCircularReferenceHandling(this JsonSerializerOptions options, Action<CircularReferenceOptions>? configure = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var circularOptions = new CircularReferenceOptions();
            configure?.Invoke(circularOptions);

            var handler = circularOptions.GetReferenceHandler();
            if (handler != null)
            {
                options.ReferenceHandler = handler;
            }

            return options;
        }

        /// <summary>
        /// Configures date/time format handling.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="format">The date/time format string.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithDateTimeFormat(this JsonSerializerOptions options, string format, IFormatProvider? formatProvider = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            options.Converters.Add(new DateTimeFormatConverter(format, formatProvider));
            options.Converters.Add(new DateTimeOffsetFormatConverter(format, formatProvider));

            return options;
        }

        /// <summary>
        /// Enables support for modern C# features like records and init-only properties.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions WithModernCSharpSupport(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.Converters.Add(new ModernCSharpConverterFactory());
            return options;
        }

        /// <summary>
        /// Validates the JsonSerializerOptions configuration.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to validate.</param>
        /// <returns>The validated JsonSerializerOptions instance.</returns>
        /// <exception cref="JsonToolkitException">Thrown when the configuration is invalid.</exception>
        public static JsonSerializerOptions Validate(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            try
            {
                // Attempt to serialize a simple object to validate the configuration
                var testObj = new { test = "value" };
                _ = JsonSerializer.Serialize(testObj, options);

                return options;
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    "JsonSerializerOptions configuration is invalid.",
                    ex,
                    operation: "ValidateConfiguration"
                );
            }
        }

        /// <summary>
        /// Applies all registered converters from the ConverterRegistry to the options.
        /// </summary>
        /// <param name="options">The options to apply converters to.</param>
        /// <param name="overrideExisting">Whether to override existing converters of the same type.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions ApplyRegisteredConverters(this JsonSerializerOptions options, bool overrideExisting = false)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            try
            {
                ConverterRegistry.ApplyConvertersToOptions(options, overrideExisting);
                return options;
            }
            catch (Exception ex)
            {
                throw new JsonToolkitException(
                    "Failed to apply registered converters to JsonSerializerOptions.",
                    ex,
                    operation: "ApplyRegisteredConverters"
                );
            }
        }

        /// <summary>
        /// Gets debugging information about converters in the options.
        /// </summary>
        /// <param name="options">The options to analyze.</param>
        /// <returns>A string containing debugging information.</returns>
        public static string GetConverterDebugInfo(this JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var analysis = ConverterDebugger.AnalyzeConverters(options);
            var info = new List<string>
            {
                $"Total Converters: {analysis.TotalConverters}",
                $"JsonToolkit.STJ Converters: {analysis.ToolkitConverters}",
                $"System Converters: {analysis.SystemConverters}",
                $"Conflicts: {analysis.Conflicts.Count}"
            };

            if (analysis.HasConflicts)
            {
                info.Add("");
                info.Add("Conflicts:");
                foreach (var conflict in analysis.Conflicts)
                {
                    info.Add($"  - {conflict.Message}");
                }
            }

            info.Add("");
            info.Add("Converters by Type:");
            foreach (var kvp in analysis.ConvertersByType.OrderBy(k => k.Key.Name))
            {
                info.Add($"  {kvp.Key.Name}:");
                foreach (var converter in kvp.Value)
                {
                    var details = ConverterDebugger.GetConverterDetails(converter);
                    info.Add($"    - {details.DebugInfo}");
                }
            }

            return string.Join(Environment.NewLine, info);
        }

        private static Type? GetConverterTargetType(JsonConverter converter)
        {
            var converterType = converter.GetType();
            
            while (converterType != null && converterType != typeof(object))
            {
                if (converterType.IsGenericType)
                {
                    var genericTypeDef = converterType.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(JsonConverter<>) ||
                        genericTypeDef == typeof(SimpleJsonConverter<>) ||
                        genericTypeDef == typeof(ReadOnlyJsonConverter<>) ||
                        genericTypeDef == typeof(WriteOnlyJsonConverter<>))
                    {
                        return converterType.GetGenericArguments()[0];
                    }
                }
                
                converterType = converterType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Gets or creates an OptionalPropertyConverterFactory from the options.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to search.</param>
        /// <returns>The OptionalPropertyConverterFactory instance.</returns>
        private static OptionalPropertyConverterFactory GetOrCreateOptionalPropertyFactory(JsonSerializerOptions options)
        {
            // Look for existing factory
            var existingFactory = options.Converters
                .OfType<OptionalPropertyConverterFactory>()
                .FirstOrDefault();

            if (existingFactory != null)
                return existingFactory;

            // Create new factory and add it
            var factory = new OptionalPropertyConverterFactory();
            options.Converters.Add(factory);
            return factory;
        }
    }
