namespace JsonToolkit.STJ;

/// <summary>
/// Provides deep merge functionality for JSON objects and .NET objects.
/// </summary>
public static class JsonMerge
{
        /// <summary>
        /// Performs a deep merge of two JsonElement objects.
        /// </summary>
        /// <param name="target">The target JsonElement to merge into.</param>
        /// <param name="source">The source JsonElement to merge from.</param>
        /// <returns>A new JsonElement containing the merged result.</returns>
        /// <exception cref="JsonToolkitException">Thrown when merge operation fails.</exception>
        public static JsonElement DeepMerge(JsonElement target, JsonElement source)
        {
            try
            {
                return DeepMergeInternal(target, source);
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    "Failed to perform deep merge operation on JsonElement objects.",
                    ex,
                    operation: "DeepMerge"
                );
            }
        }

        /// <summary>
        /// Performs a deep merge of multiple JsonElement objects.
        /// </summary>
        /// <param name="sources">The JsonElement objects to merge, in order of precedence.</param>
        /// <returns>A new JsonElement containing the merged result.</returns>
        /// <exception cref="ArgumentException">Thrown when no sources are provided.</exception>
        /// <exception cref="JsonToolkitException">Thrown when merge operation fails.</exception>
        public static JsonElement DeepMerge(params JsonElement[] sources)
        {
            if (sources == null || sources.Length == 0)
                throw new ArgumentException("At least one source element must be provided.", nameof(sources));

            if (sources.Length == 1)
                return sources[0];

            try
            {
                var result = sources[0];
                for (int i = 1; i < sources.Length; i++)
                {
                    result = DeepMergeInternal(result, sources[i]);
                }
                return result;
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    "Failed to perform deep merge operation on multiple JsonElement objects.",
                    ex,
                    operation: "DeepMerge"
                );
            }
        }

        /// <summary>
        /// Performs a deep merge of two .NET objects through JSON serialization.
        /// </summary>
        /// <typeparam name="T">The type of the objects to merge.</typeparam>
        /// <param name="target">The target object to merge into.</param>
        /// <param name="source">The source object to merge from.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <returns>A new object of type T containing the merged result.</returns>
        /// <exception cref="JsonToolkitException">Thrown when merge operation fails.</exception>
        public static T DeepMerge<T>(T target, T source, JsonSerializerOptions? options = null)
        {
            if (target == null && source == null)
                return default(T)!;
            
            if (target == null)
                return source;
            
            if (source == null)
                return target;

            try
            {
                // Serialize both objects to JsonElement
                var targetJson = JsonSerializer.SerializeToElement(target, options);
                var sourceJson = JsonSerializer.SerializeToElement(source, options);

                // Perform deep merge
                var mergedJson = DeepMergeInternal(targetJson, sourceJson);

                // Deserialize back to the target type
                return JsonSerializer.Deserialize<T>(mergedJson, options)!;
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    $"Failed to perform deep merge operation on objects of type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeepMerge"
                );
            }
        }

        /// <summary>
        /// Performs a deep merge of multiple .NET objects through JSON serialization.
        /// </summary>
        /// <typeparam name="T">The type of the objects to merge.</typeparam>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <param name="sources">The objects to merge, in order of precedence.</param>
        /// <returns>A new object of type T containing the merged result.</returns>
        /// <exception cref="ArgumentException">Thrown when no sources are provided.</exception>
        /// <exception cref="JsonToolkitException">Thrown when merge operation fails.</exception>
        public static T DeepMerge<T>(JsonSerializerOptions? options, params T[] sources)
        {
            if (sources == null || sources.Length == 0)
                throw new ArgumentException("At least one source object must be provided.", nameof(sources));

            if (sources.Length == 1)
                return sources[0];

            try
            {
                var result = sources[0];
                for (int i = 1; i < sources.Length; i++)
                {
                    result = DeepMerge(result, sources[i], options);
                }
                return result;
            }
            catch (Exception ex) when (!(ex is JsonToolkitException))
            {
                throw new JsonToolkitException(
                    $"Failed to perform deep merge operation on multiple objects of type '{typeof(T).Name}'.",
                    ex,
                    operation: "DeepMerge"
                );
            }
        }

        /// <summary>
        /// Performs a deep merge of multiple .NET objects through JSON serialization using default options.
        /// </summary>
        /// <typeparam name="T">The type of the objects to merge.</typeparam>
        /// <param name="sources">The objects to merge, in order of precedence.</param>
        /// <returns>A new object of type T containing the merged result.</returns>
        /// <exception cref="ArgumentException">Thrown when no sources are provided.</exception>
        /// <exception cref="JsonToolkitException">Thrown when merge operation fails.</exception>
        public static T DeepMerge<T>(params T[] sources)
        {
            return DeepMerge<T>(null, sources);
        }

        /// <summary>
        /// Internal implementation of deep merge logic for JsonElement objects.
        /// </summary>
        /// <param name="target">The target JsonElement to merge into.</param>
        /// <param name="source">The source JsonElement to merge from.</param>
        /// <returns>A new JsonElement containing the merged result.</returns>
        private static JsonElement DeepMergeInternal(JsonElement target, JsonElement source)
        {
            // If source is null, undefined, or the target is not an object, source wins
            if (source.ValueKind == JsonValueKind.Null || 
                source.ValueKind == JsonValueKind.Undefined ||
                target.ValueKind != JsonValueKind.Object ||
                source.ValueKind != JsonValueKind.Object)
            {
                return source.Clone();
            }

            // Both are objects - perform deep merge
            var mergedProperties = new Dictionary<string, JsonElement>();

            // Add all properties from target first
            foreach (var targetProperty in target.EnumerateObject())
            {
                mergedProperties[targetProperty.Name] = targetProperty.Value.Clone();
            }

            // Merge properties from source
            foreach (var sourceProperty in source.EnumerateObject())
            {
                var propertyName = sourceProperty.Name;
                var sourceValue = sourceProperty.Value;

                if (mergedProperties.TryGetValue(propertyName, out var targetValue))
                {
                    // Property exists in both - check if we need to merge recursively
                    if (targetValue.ValueKind == JsonValueKind.Object && 
                        sourceValue.ValueKind == JsonValueKind.Object)
                    {
                        // Both are objects - recursive merge
                        mergedProperties[propertyName] = DeepMergeInternal(targetValue, sourceValue);
                    }
                    else
                    {
                        // Different types or non-objects - source wins (including arrays)
                        mergedProperties[propertyName] = sourceValue.Clone();
                    }
                }
                else
                {
                    // Property only exists in source - add it
                    mergedProperties[propertyName] = sourceValue.Clone();
                }
            }

            // Convert back to JsonElement
            return CreateJsonElementFromDictionary(mergedProperties);
        }

        /// <summary>
        /// Creates a JsonElement from a dictionary of properties.
        /// </summary>
        /// <param name="properties">The properties to include in the JsonElement.</param>
        /// <returns>A new JsonElement containing the properties.</returns>
        private static JsonElement CreateJsonElementFromDictionary(Dictionary<string, JsonElement> properties)
        {
            // Pre-size the stream to reduce allocations - estimate based on property count
            var estimatedSize = properties.Count * 50; // Rough estimate: 50 bytes per property
            using var stream = new System.IO.MemoryStream(estimatedSize);
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            foreach (var kvp in properties)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();

            var jsonBytes = stream.ToArray();
            var document = JsonDocument.Parse(jsonBytes);
            return document.RootElement.Clone();
        }
    }
