using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Factory for creating OptionalPropertyConverter instances.
    /// Manages converter instances and provides type-safe creation methods.
    /// </summary>
    public class OptionalPropertyConverterFactory : JsonConverterFactory
    {
        private readonly ConcurrentDictionary<Type, JsonConverter> _converterCache;
        private readonly ConcurrentDictionary<Type, object> _defaultsCache;

        /// <summary>
        /// Initializes a new instance of the OptionalPropertyConverterFactory class.
        /// </summary>
        public OptionalPropertyConverterFactory()
        {
            _converterCache = new ConcurrentDictionary<Type, JsonConverter>();
            _defaultsCache = new ConcurrentDictionary<Type, object>();
        }

        /// <summary>
        /// Determines whether the converter instance can convert the specified object type.
        /// </summary>
        /// <param name="typeToConvert">The type of object to check.</param>
        /// <returns>True if the instance can convert the specified object type; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            // Only convert reference types that have a parameterless constructor
            return typeToConvert.IsClass && 
                   !typeToConvert.IsAbstract && 
                   typeToConvert.GetConstructor(Type.EmptyTypes) != null &&
                   _defaultsCache.ContainsKey(typeToConvert);
        }

        /// <summary>
        /// Creates a converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type handled by the converter.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>A converter for the specified type.</returns>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return _converterCache.GetOrAdd(typeToConvert, type =>
            {
                if (!_defaultsCache.TryGetValue(type, out var defaults))
                {
                    throw new JsonToolkitException(
                        $"No defaults configuration found for type '{type.Name}'. " +
                        "Register defaults using RegisterDefaults<T>() before using the converter.",
                        operation: "CreateConverter"
                    );
                }

                // Create the generic converter type
                var converterType = typeof(OptionalPropertyConverter<>).MakeGenericType(type);
                
                try
                {
                    return (JsonConverter)Activator.CreateInstance(converterType, defaults)!;
                }
                catch (Exception ex)
                {
                    throw new JsonToolkitException(
                        $"Failed to create OptionalPropertyConverter for type '{type.Name}'.",
                        ex,
                        operation: "CreateConverter"
                    );
                }
            });
        }

        /// <summary>
        /// Registers default values for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to register defaults for.</typeparam>
        /// <param name="defaults">The defaults configuration.</param>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory RegisterDefaults<T>(OptionalPropertyDefaults<T> defaults) where T : class, new()
        {
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            // Validate the defaults configuration
            defaults.Validate();

            _defaultsCache[typeof(T)] = defaults;
            
            // Clear any cached converter for this type to force recreation
            _converterCache.TryRemove(typeof(T), out _);

            return this;
        }

        /// <summary>
        /// Registers default values for a specific type using a default value template.
        /// </summary>
        /// <typeparam name="T">The type to register defaults for.</typeparam>
        /// <param name="defaultValue">The default value template.</param>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory RegisterDefaults<T>(T defaultValue) where T : class, new()
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            var defaults = new OptionalPropertyDefaults<T>(defaultValue);
            return RegisterDefaults(defaults);
        }

        /// <summary>
        /// Registers default values for a specific type using a configuration action.
        /// </summary>
        /// <typeparam name="T">The type to register defaults for.</typeparam>
        /// <param name="configure">Action to configure the defaults.</param>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory RegisterDefaults<T>(Action<OptionalPropertyDefaults<T>> configure) where T : class, new()
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var defaults = new OptionalPropertyDefaults<T>();
            configure(defaults);
            return RegisterDefaults(defaults);
        }

        /// <summary>
        /// Checks if defaults are registered for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <returns>True if defaults are registered, false otherwise.</returns>
        public bool HasDefaults<T>() where T : class
        {
            return _defaultsCache.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Checks if defaults are registered for the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if defaults are registered, false otherwise.</returns>
        public bool HasDefaults(Type type)
        {
            return _defaultsCache.ContainsKey(type);
        }

        /// <summary>
        /// Removes defaults registration for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to remove defaults for.</typeparam>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory RemoveDefaults<T>() where T : class
        {
            _defaultsCache.TryRemove(typeof(T), out _);
            _converterCache.TryRemove(typeof(T), out _);
            return this;
        }

        /// <summary>
        /// Removes defaults registration for the specified type.
        /// </summary>
        /// <param name="type">The type to remove defaults for.</param>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory RemoveDefaults(Type type)
        {
            _defaultsCache.TryRemove(type, out _);
            _converterCache.TryRemove(type, out _);
            return this;
        }

        /// <summary>
        /// Clears all registered defaults and cached converters.
        /// </summary>
        /// <returns>The current factory instance for method chaining.</returns>
        public OptionalPropertyConverterFactory ClearAll()
        {
            _defaultsCache.Clear();
            _converterCache.Clear();
            return this;
        }

        /// <summary>
        /// Gets the number of types with registered defaults.
        /// </summary>
        public int RegisteredTypesCount => _defaultsCache.Count;

        /// <summary>
        /// Gets debugging information about registered types and cached converters.
        /// </summary>
        /// <returns>A string containing debugging information.</returns>
        public string GetDebugInfo()
        {
            return $"OptionalPropertyConverterFactory: {RegisteredTypesCount} registered types, {_converterCache.Count} cached converters";
        }
    }
}