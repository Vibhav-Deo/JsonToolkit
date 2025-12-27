using System.Linq;
using System.Reflection;
using JsonToolkit.STJ.Converters;
using JsonToolkit.STJ.ValidationAttributes;

namespace JsonToolkit.STJ;

/// <summary>
/// Extension methods for enabling and working with JSON validation.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Enables automatic validation during deserialization using validation attributes.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions to configure.</param>
    /// <param name="validationOptions">Optional validation options. If null, default options are used.</param>
    /// <returns>The configured JsonSerializerOptions for method chaining.</returns>
    public static JsonSerializerOptions WithValidation(
        this JsonSerializerOptions options,
        ValidationOptions? validationOptions = null)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        validationOptions ??= ValidationOptions.Enabled();

        // Remove any existing validation converter factory to avoid duplicates
        var existingFactory = options.Converters.OfType<ValidationConverterFactory>().FirstOrDefault();
        if (existingFactory != null)
        {
            options.Converters.Remove(existingFactory);
        }

        // Add the validation converter factory
        options.Converters.Add(new ValidationConverterFactory(validationOptions));

        return options;
    }

    /// <summary>
    /// Explicitly disables automatic validation during deserialization for performance-critical scenarios.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions to configure.</param>
    /// <returns>The configured JsonSerializerOptions for method chaining.</returns>
    public static JsonSerializerOptions WithoutValidation(this JsonSerializerOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Remove any existing validation converter factory
        var existingFactories = options.Converters.OfType<ValidationConverterFactory>().ToList();
        foreach (var factory in existingFactories)
        {
            options.Converters.Remove(factory);
        }

        return options;
    }

    /// <summary>
    /// Enables automatic validation during deserialization with custom configuration.
    /// </summary>
    /// <param name="options">The JsonSerializerOptions to configure.</param>
    /// <param name="configure">Action to configure validation options.</param>
    /// <returns>The configured JsonSerializerOptions for method chaining.</returns>
    public static JsonSerializerOptions WithValidation(
        this JsonSerializerOptions options,
        Action<ValidationOptions> configure)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var validationOptions = new ValidationOptions();
        configure(validationOptions);

        return options.WithValidation(validationOptions);
    }

    /// <summary>
    /// Deserializes JSON with automatic validation using validation attributes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JsonSerializerOptions. If null, default options with validation are used.</param>
    /// <returns>The deserialized and validated object.</returns>
    /// <exception cref="JsonValidationException">Thrown when validation fails.</exception>
    public static T ValidateAndDeserialize<T>(this string json, JsonSerializerOptions? options = null) where T : class
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        options ??= new JsonSerializerOptions().WithValidation();

        // Ensure validation is enabled
        if (!HasValidationConverter(options))
        {
            options = new JsonSerializerOptions(options).WithValidation();
        }

        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    /// <summary>
    /// Validates an object against its validation attributes without JSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <returns>A ValidationResult containing any validation errors.</returns>
    public static ValidationResult Validate<T>(this T? obj) where T : class
    {
        if (obj == null)
            return ValidationResult.Success();

        var errors = new List<ValidationError>();
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
                try
                {
                    var value = property.GetValue(obj);
                    var propertyPath = property.Name;

                    foreach (var validationAttribute in validationAttributes)
                    {
                        var error = validationAttribute.Validate(value, property.Name, propertyPath);
                        if (error != null)
                        {
                            errors.Add(error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError(
                        property.Name,
                        $"Error accessing property '{property.Name}' for validation: {ex.Message}",
                        "PropertyAccessError"
                    ));
                }
            }
        }

        return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    /// <summary>
    /// Validates an object and throws an exception if validation fails.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <exception cref="JsonValidationException">Thrown when validation fails.</exception>
    public static void ValidateAndThrow<T>(this T? obj) where T : class
    {
        var result = obj.Validate();
        if (!result.IsValid)
        {
            throw new JsonValidationException(
                $"Validation failed for object of type '{typeof(T).Name}'.",
                result.Errors
            );
        }
    }

    /// <summary>
    /// Checks if the JsonSerializerOptions has a validation converter configured.
    /// </summary>
    /// <param name="options">The options to check.</param>
    /// <returns>True if validation is configured; otherwise, false.</returns>
    private static bool HasValidationConverter(JsonSerializerOptions options)
    {
        return options.Converters.Any(c => c is ValidationConverterFactory);
    }
}