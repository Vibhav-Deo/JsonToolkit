using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonToolkit.STJ.Extensions;

/// <summary>
/// Provides Newtonsoft.Json-style extension methods for string deserialization.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Deserializes the JSON string to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static T FromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    /// <summary>
    /// Deserializes the JSON string to an object of the specified type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or type is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static object? FromJson(this string json, Type type, JsonSerializerOptions? options = null)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return JsonSerializer.Deserialize(json, type, options);
    }
}