namespace JsonToolkit.STJ;

/// <summary>
/// Configuration class for optional property defaults.
/// Defines default values to apply when properties are missing from JSON during deserialization.
/// </summary>
/// <typeparam name="T">The type to configure defaults for.</typeparam>
public class OptionalPropertyDefaults<T> where T : class
{
    /// <summary>
    /// Gets or sets the default value object to use as a template.
    /// Properties from this object will be used as defaults when they are missing from JSON.
    /// </summary>
    public T? DefaultValue { get; set; }

    /// <summary>
    /// Gets the dictionary of property-specific default values.
    /// Keys are property names, values are the default values to apply.
    /// </summary>
    public Dictionary<string, object?> PropertyDefaults { get; }

    /// <summary>
    /// Gets or sets a value indicating whether null values in JSON should be ignored
    /// and replaced with defaults. When true, explicit null values will be replaced
    /// with configured defaults. When false, explicit null values take precedence.
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether defaults should be applied recursively
    /// to nested objects. When true, nested objects will also have their defaults applied.
    /// </summary>
    public bool ApplyRecursively { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON values should always take precedence
    /// over configured defaults. When true (default), JSON values override defaults.
    /// When false, defaults can override JSON values.
    /// </summary>
    public bool JsonValuesPrecedence { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the OptionalPropertyDefaults class.
    /// </summary>
    public OptionalPropertyDefaults()
    {
        PropertyDefaults = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        IgnoreNullValues = false;
        ApplyRecursively = true;
    }

    /// <summary>
    /// Initializes a new instance of the OptionalPropertyDefaults class with a default value template.
    /// </summary>
    /// <param name="defaultValue">The default value object to use as a template.</param>
    public OptionalPropertyDefaults(T defaultValue) : this()
    {
        DefaultValue = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
    }

    /// <summary>
    /// Sets a default value for a specific property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="defaultValue">The default value to use.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> SetDefault(string propertyName, object? defaultValue)
    {
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

        PropertyDefaults[propertyName] = defaultValue;
        return this;
    }

    /// <summary>
    /// Sets multiple default values from a dictionary.
    /// </summary>
    /// <param name="defaults">Dictionary of property names and their default values.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> SetDefaults(Dictionary<string, object?> defaults)
    {
        if (defaults == null)
            throw new ArgumentNullException(nameof(defaults));

        foreach (var kvp in defaults)
        {
            PropertyDefaults[kvp.Key] = kvp.Value;
        }

        return this;
    }

    /// <summary>
    /// Configures whether null values in JSON should be ignored and replaced with defaults.
    /// </summary>
    /// <param name="ignoreNulls">True to ignore null values, false to respect them.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> WithNullHandling(bool ignoreNulls)
    {
        IgnoreNullValues = ignoreNulls;
        return this;
    }

    /// <summary>
    /// Configures whether defaults should be applied recursively to nested objects.
    /// </summary>
    /// <param name="applyRecursively">True to apply recursively, false to apply only to top-level properties.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> WithRecursiveApplication(bool applyRecursively)
    {
        ApplyRecursively = applyRecursively;
        return this;
    }

    /// <summary>
    /// Configures whether JSON values should take precedence over defaults.
    /// </summary>
    /// <param name="jsonPrecedence">True if JSON values should override defaults, false otherwise.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> WithJsonPrecedence(bool jsonPrecedence)
    {
        JsonValuesPrecedence = jsonPrecedence;
        return this;
    }

    /// <summary>
    /// Removes a default value for a specific property.
    /// </summary>
    /// <param name="propertyName">The name of the property to remove defaults for.</param>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> RemoveDefault(string propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName))
        {
            PropertyDefaults.Remove(propertyName);
        }

        return this;
    }

    /// <summary>
    /// Clears all configured property defaults.
    /// </summary>
    /// <returns>The current OptionalPropertyDefaults instance for method chaining.</returns>
    public OptionalPropertyDefaults<T> ClearDefaults()
    {
        PropertyDefaults.Clear();
        return this;
    }

    /// <summary>
    /// Checks if a default value is configured for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if a default is configured, false otherwise.</returns>
    public bool HasDefault(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && PropertyDefaults.ContainsKey(propertyName);
    }

    /// <summary>
    /// Gets the default value for the specified property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The default value, or null if no default is configured.</returns>
    public object? GetDefault(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        PropertyDefaults.TryGetValue(propertyName, out var value);
        return value;
    }

    /// <summary>
    /// Creates a copy of this OptionalPropertyDefaults configuration.
    /// </summary>
    /// <returns>A new OptionalPropertyDefaults instance with the same configuration.</returns>
    public OptionalPropertyDefaults<T> Clone()
    {
        var clone = new OptionalPropertyDefaults<T>(DefaultValue!)
        {
            IgnoreNullValues = IgnoreNullValues,
            ApplyRecursively = ApplyRecursively,
            JsonValuesPrecedence = JsonValuesPrecedence
        };

        foreach (var kvp in PropertyDefaults)
        {
            clone.PropertyDefaults[kvp.Key] = kvp.Value;
        }

        return clone;
    }

    /// <summary>
    /// Validates the configuration for consistency and correctness.
    /// </summary>
    /// <exception cref="JsonToolkitException">Thrown when the configuration is invalid.</exception>
    public void Validate()
    {
        try
        {
            // Validate that property names exist on the target type
            var targetType = typeof(T);
            var properties = targetType.GetProperties();
            var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in properties)
            {
                propertyNames.Add(property.Name);
            }

            foreach (var propertyName in PropertyDefaults.Keys)
            {
                if (!propertyNames.Contains(propertyName))
                {
                    throw new JsonToolkitException(
                        $"Property '{propertyName}' does not exist on type '{targetType.Name}'.",
                        propertyPath: propertyName,
                        operation: "ValidateConfiguration"
                    );
                }
            }
        }
        catch (Exception ex) when (!(ex is JsonToolkitException))
        {
            throw new JsonToolkitException(
                $"Failed to validate OptionalPropertyDefaults configuration for type '{typeof(T).Name}'.",
                ex,
                operation: "ValidateConfiguration"
            );
        }
    }
}