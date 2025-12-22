using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonToolkit.STJ.Extensions;

/// <summary>
/// Provides Newtonsoft.Json-style extension methods for object serialization.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Serializes the object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public static string ToJson<T>(this T obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Serializes the object to a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>A UTF-8 encoded JSON byte array.</returns>
    public static byte[] ToJsonBytes<T>(this T obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, options);
    }

    /// <summary>
    /// Creates a deep clone of the object using JSON serialization round-trip.
    /// </summary>
    /// <typeparam name="T">The type of the object to clone.</typeparam>
    /// <param name="obj">The object to clone.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>A deep clone of the object.</returns>
    public static T DeepClone<T>(this T obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
        {
            return default!;
        }

        var json = JsonSerializer.Serialize(obj, options);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    /// <summary>
    /// Asynchronously serializes the object to a stream.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ToJsonAsync<T>(
        this T obj,
        Stream stream,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, obj, options, cancellationToken);
    }
}
