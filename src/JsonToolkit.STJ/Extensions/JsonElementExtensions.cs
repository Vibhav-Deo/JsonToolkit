using System.Linq;

namespace JsonToolkit.STJ.Extensions;

/// <summary>
/// Extension methods for JsonElement to provide additional functionality.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Safely gets a string value from a JsonElement, returning null if not a string.
    /// </summary>
    /// <param name="element">The JsonElement to get the string from.</param>
    /// <returns>The string value or null if not a string.</returns>
    public static string? GetStringOrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <summary>
    /// Safely gets an int value from a JsonElement, returning null if not a number or can't convert.
    /// </summary>
    /// <param name="element">The JsonElement to get the int from.</param>
    /// <returns>The int value or null if not convertible.</returns>
    public static int? GetInt32OrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) ? value : null;
    }

    /// <summary>
    /// Safely gets a double value from a JsonElement, returning null if not a number.
    /// </summary>
    /// <param name="element">The JsonElement to get the double from.</param>
    /// <returns>The double value or null if not a number.</returns>
    public static double? GetDoubleOrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : null;
    }

    /// <summary>
    /// Safely gets a boolean value from a JsonElement, returning null if not a boolean.
    /// </summary>
    /// <param name="element">The JsonElement to get the boolean from.</param>
    /// <returns>The boolean value or null if not a boolean.</returns>
    public static bool? GetBooleanOrNull(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    /// <summary>
    /// Checks if the JsonElement has a property with the specified name.
    /// </summary>
    /// <param name="element">The JsonElement to check.</param>
    /// <param name="propertyName">The property name to look for.</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public static bool HasProperty(this JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out _);
    }

    /// <summary>
    /// Gets a property value safely, returning null if the property doesn't exist.
    /// </summary>
    /// <param name="element">The JsonElement to get the property from.</param>
    /// <param name="propertyName">The property name to get.</param>
    /// <returns>The property value or null if it doesn't exist.</returns>
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) 
            ? value : null;
    }

    /// <summary>
    /// Converts the JsonElement to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="element">The JsonElement to convert.</param>
    /// <param name="options">Optional JsonSerializerOptions.</param>
    /// <returns>The deserialized object.</returns>
    public static T? ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(element, options);
    }

    /// <summary>
    /// Gets the count of elements in an array or properties in an object.
    /// </summary>
    /// <param name="element">The JsonElement to count.</param>
    /// <returns>The count of elements/properties, or 0 for other types.</returns>
    public static int GetCount(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Array => element.GetArrayLength(),
            JsonValueKind.Object => element.EnumerateObject().Count(),
            _ => 0
        };
    }
}