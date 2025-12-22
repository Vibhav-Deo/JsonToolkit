using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Registry for managing JSON converter registration and precedence.
    /// Provides deterministic converter ordering and conflict detection.
    /// </summary>
    public static class ConverterRegistry
    {
        private static readonly Dictionary<Type, List<ConverterInfo>> _convertersByType = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Registers a converter with the registry.
        /// </summary>
        /// <param name="converter">The converter to register.</param>
        /// <param name="precedence">The precedence of the converter (higher values have higher precedence).</param>
        /// <exception cref="ArgumentNullException">Thrown when converter is null.</exception>
        public static void RegisterConverter(JsonConverter converter, int precedence = 0)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            lock (_lock)
            {
                var targetType = GetConverterTargetType(converter);
                if (targetType == null)
                    return;

                if (!_convertersByType.TryGetValue(targetType, out var converters))
                {
                    converters = new List<ConverterInfo>();
                    _convertersByType[targetType] = converters;
                }

                var converterInfo = new ConverterInfo(converter, precedence);
                
                // Check for exact same converter instance (not just same type)
                var existingConverter = converters.FirstOrDefault(c => 
                    ReferenceEquals(c.Converter, converter));
                
                if (existingConverter != null)
                {
                    throw new JsonToolkitException(
                        $"The exact same converter instance is already registered for type '{targetType.Name}'. " +
                        "Remove the existing converter before registering it again.",
                        operation: "RegisterConverter"
                    );
                }

                converters.Add(converterInfo);
                
                // Sort by precedence (highest first), then by registration order for tiebreaking
                converters.Sort((a, b) => 
                {
                    var precedenceComparison = b.Precedence.CompareTo(a.Precedence);
                    return precedenceComparison != 0 ? precedenceComparison : a.RegistrationOrder.CompareTo(b.RegistrationOrder);
                });
            }
        }

        /// <summary>
        /// Unregisters a converter from the registry.
        /// </summary>
        /// <param name="converter">The converter to unregister.</param>
        /// <returns>True if the converter was found and removed; otherwise, false.</returns>
        public static bool UnregisterConverter(JsonConverter converter)
        {
            if (converter == null)
                return false;

            lock (_lock)
            {
                var targetType = GetConverterTargetType(converter);
                if (targetType == null || !_convertersByType.TryGetValue(targetType, out var converters))
                    return false;

                var removed = converters.RemoveAll(c => ReferenceEquals(c.Converter, converter)) > 0;
                
                if (converters.Count == 0)
                {
                    _convertersByType.Remove(targetType);
                }

                return removed;
            }
        }

        /// <summary>
        /// Gets all registered converters for a specific type, ordered by precedence.
        /// </summary>
        /// <param name="type">The type to get converters for.</param>
        /// <returns>An enumerable of converters ordered by precedence (highest first).</returns>
        public static IEnumerable<JsonConverter> GetConvertersForType(Type type)
        {
            if (type == null)
                return Enumerable.Empty<JsonConverter>();

            lock (_lock)
            {
                if (_convertersByType.TryGetValue(type, out var converters))
                {
                    return converters.Select(c => c.Converter).ToList();
                }

                return Enumerable.Empty<JsonConverter>();
            }
        }

        /// <summary>
        /// Gets the highest precedence converter for a specific type.
        /// </summary>
        /// <param name="type">The type to get the converter for.</param>
        /// <returns>The highest precedence converter, or null if none found.</returns>
        public static JsonConverter? GetPrimaryConverterForType(Type type)
        {
            return GetConvertersForType(type).FirstOrDefault();
        }

        /// <summary>
        /// Applies registered converters to JsonSerializerOptions in precedence order.
        /// </summary>
        /// <param name="options">The options to apply converters to.</param>
        /// <param name="overrideExisting">Whether to override existing converters of the same type.</param>
        public static void ApplyConvertersToOptions(JsonSerializerOptions options, bool overrideExisting = false)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (_lock)
            {
                var allConverters = new List<(JsonConverter Converter, int Precedence)>();

                foreach (var kvp in _convertersByType)
                {
                    foreach (var converterInfo in kvp.Value)
                    {
                        allConverters.Add((converterInfo.Converter, converterInfo.Precedence));
                    }
                }

                // Sort all converters by precedence
                allConverters.Sort((a, b) => b.Precedence.CompareTo(a.Precedence));

                foreach (var (converter, _) in allConverters)
                {
                    var targetType = GetConverterTargetType(converter);
                    if (targetType == null)
                        continue;

                    // Check if a converter for this type already exists
                    var existingConverter = options.Converters.FirstOrDefault(c => 
                        GetConverterTargetType(c) == targetType);

                    if (existingConverter != null)
                    {
                        if (overrideExisting)
                        {
                            options.Converters.Remove(existingConverter);
                            options.Converters.Add(converter);
                        }
                        // If not overriding, skip this converter
                    }
                    else
                    {
                        options.Converters.Add(converter);
                    }
                }
            }
        }

        /// <summary>
        /// Gets debugging information about all registered converters.
        /// </summary>
        /// <returns>A string containing debugging information.</returns>
        public static string GetDebugInfo()
        {
            lock (_lock)
            {
                var info = new List<string>();
                
                foreach (var kvp in _convertersByType.OrderBy(k => k.Key.Name))
                {
                    info.Add($"Type: {kvp.Key.Name}");
                    
                    foreach (var converterInfo in kvp.Value)
                    {
                        var debugInfo = converterInfo.Converter switch
                        {
                            SimpleJsonConverter<object> simple => simple.GetDebugInfo(),
                            ReadOnlyJsonConverter<object> readOnly => readOnly.GetDebugInfo(),
                            WriteOnlyJsonConverter<object> writeOnly => writeOnly.GetDebugInfo(),
                            _ => $"Converter: {converterInfo.Converter.GetType().Name}, Precedence: {converterInfo.Precedence}"
                        };
                        
                        info.Add($"  - {debugInfo} (Order: {converterInfo.RegistrationOrder})");
                    }
                }

                return string.Join(Environment.NewLine, info);
            }
        }

        /// <summary>
        /// Clears all registered converters.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _convertersByType.Clear();
            }
        }

        /// <summary>
        /// Gets the number of registered converters.
        /// </summary>
        /// <returns>The total number of registered converters.</returns>
        public static int Count
        {
            get
            {
                lock (_lock)
                {
                    return _convertersByType.Values.Sum(list => list.Count);
                }
            }
        }

        /// <summary>
        /// Checks if there are any converter conflicts for the given type.
        /// </summary>
        /// <param name="type">The type to check for conflicts.</param>
        /// <returns>True if there are multiple converters with the same precedence; otherwise, false.</returns>
        public static bool HasConverterConflicts(Type type)
        {
            if (type == null)
                return false;

            lock (_lock)
            {
                if (!_convertersByType.TryGetValue(type, out var converters) || converters.Count <= 1)
                    return false;

                // Check if there are multiple converters with the same highest precedence
                var highestPrecedence = converters[0].Precedence;
                return converters.Count(c => c.Precedence == highestPrecedence) > 1;
            }
        }

        private static Type? GetConverterTargetType(JsonConverter converter)
        {
            var converterType = converter.GetType();
            
            // Look for JsonConverter<T> base class
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

        private class ConverterInfo
        {
            private static int _nextRegistrationOrder = 0;

            public JsonConverter Converter { get; }
            public int Precedence { get; }
            public int RegistrationOrder { get; }

            public ConverterInfo(JsonConverter converter, int precedence)
            {
                Converter = converter;
                Precedence = precedence;
                RegistrationOrder = System.Threading.Interlocked.Increment(ref _nextRegistrationOrder);
            }
        }
    }
}