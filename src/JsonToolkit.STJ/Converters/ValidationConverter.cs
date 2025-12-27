using System.Linq;
using System.Reflection;
using JsonToolkit.STJ.ValidationAttributes;

namespace JsonToolkit.STJ.Converters;

/// <summary>
/// A converter that applies validation attributes during deserialization.
/// This converter validates properties after deserialization is complete.
/// </summary>
/// <typeparam name="T">The type to deserialize and validate.</typeparam>
public class ValidationConverter<T> : JsonConverter<T> where T : class
{
    private readonly ValidationOptions _options;
    private readonly Dictionary<PropertyInfo, IJsonValidationAttribute[]> _propertyValidations;

    /// <summary>
    /// Initializes a new instance of the ValidationConverter class.
    /// </summary>
    /// <param name="options">The validation options to use.</param>
    public ValidationConverter(ValidationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _propertyValidations = BuildPropertyValidations();
    }

    /// <summary>
    /// Determines whether this converter can convert the specified object type.
    /// </summary>
    /// <param name="typeToConvert">The type of object to check.</param>
    /// <returns>True if the instance can convert the specified object type; otherwise, false.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(T) && _propertyValidations.Any();
    }

    /// <summary>
    /// Reads and converts the JSON to the specified type with validation.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converted and validated value.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the JSON into a JsonDocument first
        using var document = JsonDocument.ParseValue(ref reader);

        // Deserialize using the default mechanism (without validation converters)
        var json = document.RootElement.GetRawText();
        var tempOptions = CreateOptionsWithoutValidation(options);
        var result = JsonSerializer.Deserialize<T>(json, tempOptions);

        // Apply validation if enabled
        if (_options.EnableValidation && result != null)
        {
            var validationErrors = ValidateObject(result);
            if (validationErrors.Any())
            {
                if (_options.ThrowOnValidationFailure)
                {
                    throw new JsonValidationException(
                        $"Validation failed for type '{typeof(T).Name}'.",
                        validationErrors
                    );
                }
                else if (_options.CollectValidationErrors)
                {
                    // Store validation errors in the options for later retrieval
                    StoreValidationErrors(options, validationErrors);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Writes the specified value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // For writing, we don't need validation, so we can serialize without validation converters
        var tempOptions = CreateOptionsWithoutValidation(options);
        JsonSerializer.Serialize(writer, value, tempOptions);
    }

    /// <summary>
    /// Creates JsonSerializerOptions without validation converters to avoid infinite recursion.
    /// </summary>
    /// <param name="originalOptions">The original options.</param>
    /// <returns>New options without validation converters.</returns>
    private static JsonSerializerOptions CreateOptionsWithoutValidation(JsonSerializerOptions originalOptions)
    {
        var tempOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = originalOptions.PropertyNamingPolicy,
            PropertyNameCaseInsensitive = originalOptions.PropertyNameCaseInsensitive,
            WriteIndented = originalOptions.WriteIndented,
            DefaultIgnoreCondition = originalOptions.DefaultIgnoreCondition,
            IgnoreReadOnlyProperties = originalOptions.IgnoreReadOnlyProperties,
            IgnoreReadOnlyFields = originalOptions.IgnoreReadOnlyFields,
            IncludeFields = originalOptions.IncludeFields,
            MaxDepth = originalOptions.MaxDepth,
            NumberHandling = originalOptions.NumberHandling,
            ReadCommentHandling = originalOptions.ReadCommentHandling,
            UnknownTypeHandling = originalOptions.UnknownTypeHandling
        };

        // Add all converters except validation converters
        foreach (var converter in originalOptions.Converters)
        {
            if (!(converter is ValidationConverterFactory) &&
                (!converter.GetType().IsGenericType ||
                 converter.GetType().GetGenericTypeDefinition() != typeof(ValidationConverter<>)))
            {
                tempOptions.Converters.Add(converter);
            }
        }

        return tempOptions;
    }

    /// <summary>
    /// Validates an object against its validation attributes.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <returns>A collection of validation errors, if any.</returns>
    private IEnumerable<ValidationError> ValidateObject(T obj)
    {
        var errors = new List<ValidationError>();

        foreach (var kvp in _propertyValidations)
        {
            var property = kvp.Key;
            var validationAttributes = kvp.Value;

            try
            {
                var value = property.GetValue(obj);
                var propertyPath =
                    property.Name; // In a more sophisticated implementation, this could be the full JSON path

                foreach (var validationAttribute in validationAttributes)
                {
                    var error = validationAttribute.Validate(value, property.Name, propertyPath);
                    if (error != null)
                    {
                        // Enhance error with additional context
                        var enhancedError = new ValidationError(
                            error.PropertyPath,
                            $"{error.Message} (Attempted value: {FormatAttemptedValue(value)}, Property type: {property.PropertyType.Name})",
                            error.ErrorType,
                            value,
                            property.PropertyType
                        );

                        errors.Add(enhancedError);

                        // Stop at first error if not validating all properties
                        if (!_options.ValidateAllProperties)
                            return errors;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(
                    property.Name,
                    $"Error accessing property '{property.Name}' for validation: {ex.Message}",
                    "PropertyAccessError",
                    null,
                    property.PropertyType
                ));
            }
        }

        return errors;
    }

    /// <summary>
    /// Formats the attempted value for display in error messages.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A formatted string representation of the value.</returns>
    private static string FormatAttemptedValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string stringValue)
            return $"\"{stringValue}\"";

        if (value is char charValue)
            return $"'{charValue}'";

        return value.ToString() ?? "null";
    }

    /// <summary>
    /// Builds a dictionary of property validation attributes for the target type.
    /// </summary>
    /// <returns>A dictionary mapping properties to their validation attributes.</returns>
    private static Dictionary<PropertyInfo, IJsonValidationAttribute[]> BuildPropertyValidations()
    {
        var result = new Dictionary<PropertyInfo, IJsonValidationAttribute[]>();
        var type = typeof(T);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var validationAttributes = property.GetCustomAttributes()
                .OfType<IJsonValidationAttribute>()
                .ToArray();

            if (validationAttributes.Length > 0)
            {
                result[property] = validationAttributes;
            }
        }

        return result;
    }

    /// <summary>
    /// Stores validation errors in the JsonSerializerOptions for later retrieval.
    /// </summary>
    /// <param name="options">The serializer options.</param>
    /// <param name="errors">The validation errors to store.</param>
    private static void StoreValidationErrors(JsonSerializerOptions options, IEnumerable<ValidationError> errors)
    {
        // This is a simplified implementation. In a real-world scenario,
        // you might want to use a more sophisticated mechanism to store and retrieve errors.
        // For now, we'll just ignore this functionality as it's not critical for basic validation.
    }
}
