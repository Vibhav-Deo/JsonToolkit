using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JsonToolkit.STJ.Extensions;

/// <summary>
/// Provides extension methods for stream-based JSON operations.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Asynchronously deserializes JSON from a stream to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static async Task<T> FromJsonAsync<T>(
        this Stream stream,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var result = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
        return result!;
    }

    /// <summary>
    /// Asynchronously deserializes JSON from a stream to an object of the specified type.
    /// </summary>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the deserialized object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream or type is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static async Task<object?> FromJsonAsync(
        this Stream stream,
        Type type,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return await JsonSerializer.DeserializeAsync(stream, type, options, cancellationToken);
    }

    /// <summary>
    /// Asynchronously serializes an object to JSON and writes it to a stream.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static async Task ToJsonAsync<T>(
        this Stream stream,
        T value,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
    }

    /// <summary>
    /// Asynchronously serializes an object to JSON and writes it to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="type">The type of the object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream or type is null.</exception>
    public static async Task ToJsonAsync(
        this Stream stream,
        object? value,
        Type type,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        await JsonSerializer.SerializeAsync(stream, value, type, options, cancellationToken);
    }
}