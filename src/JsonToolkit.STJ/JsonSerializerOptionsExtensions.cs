using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ
{
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
        /// Adds a converter to the options with validation.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to enhance.</param>
        /// <param name="converter">The converter to add.</param>
        /// <returns>The enhanced JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions AddConverter(this JsonSerializerOptions options, JsonConverter converter)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            try
            {
                options.Converters.Add(converter);
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
    }
}
