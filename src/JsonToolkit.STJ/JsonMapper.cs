using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Provides object-to-object transformation capabilities through JSON serialization.
    /// </summary>
    public class JsonMapper
    {
        private readonly Dictionary<Type, Dictionary<Type, object>> _configurations = new();
        private readonly JsonSerializerOptions _options;

        private JsonMapper(JsonSerializerOptions options = null)
        {
            _options = options ?? new JsonSerializerOptions();
        }

        /// <summary>
        /// Creates a new JsonMapper instance.
        /// </summary>
        public static JsonMapper Create(JsonSerializerOptions options = null)
        {
            return new JsonMapper(options);
        }

        /// <summary>
        /// Configures mapping between source and target types.
        /// </summary>
        public JsonMapper Map<TSource, TTarget>(Action<MappingConfiguration<TSource, TTarget>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var config = new MappingConfiguration<TSource, TTarget>();
            configure(config);

            if (!_configurations.ContainsKey(typeof(TSource)))
                _configurations[typeof(TSource)] = new Dictionary<Type, object>();

            _configurations[typeof(TSource)][typeof(TTarget)] = config;
            return this;
        }

        /// <summary>
        /// Transforms a source object to target type using configured mappings.
        /// </summary>
        public TTarget Transform<TSource, TTarget>(TSource source)
        {
            if (source == null)
                return default(TTarget);

            try
            {
                var config = GetConfiguration<TSource, TTarget>();
                return config != null ? config.Transform(source, _options) : DefaultTransform<TSource, TTarget>(source);
            }
            catch (JsonMappingException)
            {
                // Re-throw JsonMappingException without wrapping to avoid double-wrapping
                throw;
            }
            catch (Exception ex)
            {
                throw new JsonMappingException(
                    $"Failed to transform {typeof(TSource).Name} to {typeof(TTarget).Name}",
                    typeof(TSource),
                    typeof(TTarget),
                    ex);
            }
        }

        /// <summary>
        /// Transforms a collection of source objects to target type using configured mappings.
        /// </summary>
        public IEnumerable<TTarget> Transform<TSource, TTarget>(IEnumerable<TSource> sources)
        {
            if (sources == null)
                return Enumerable.Empty<TTarget>();

            return sources.Select(Transform<TSource, TTarget>);
        }

        private MappingConfiguration<TSource, TTarget> GetConfiguration<TSource, TTarget>()
        {
            if (_configurations.TryGetValue(typeof(TSource), out var targetConfigs) &&
                targetConfigs.TryGetValue(typeof(TTarget), out var config))
            {
                return (MappingConfiguration<TSource, TTarget>)config;
            }
            return null;
        }

        private TTarget DefaultTransform<TSource, TTarget>(TSource source)
        {
            // Default transformation through JSON serialization
            var json = JsonSerializer.Serialize(source, _options);
            return JsonSerializer.Deserialize<TTarget>(json, _options);
        }
    }

    /// <summary>
    /// Configuration for mapping between source and target types.
    /// </summary>
    public class MappingConfiguration<TSource, TTarget>
    {
        private readonly List<MemberMapping> _memberMappings = new();
        private readonly List<ValueResolver> _valueResolvers = new();

        /// <summary>
        /// Maps a target member using a custom value resolver.
        /// </summary>
        public MappingConfiguration<TSource, TTarget> ForMember<TValue>(
            Expression<Func<TTarget, TValue>> targetMember,
            Func<TSource, TValue> valueResolver)
        {
            if (targetMember == null)
                throw new ArgumentNullException(nameof(targetMember));
            if (valueResolver == null)
                throw new ArgumentNullException(nameof(valueResolver));

            var targetName = GetMemberName(targetMember);
            _valueResolvers.Add(new ValueResolver(targetName, source => valueResolver((TSource)source)));
            return this;
        }

        internal TTarget Transform(TSource source, JsonSerializerOptions options)
        {
            try
            {
                // Serialize source to JSON
                var sourceJson = JsonSerializer.Serialize(source, options);
                var sourceElement = JsonDocument.Parse(sourceJson).RootElement;

                // Apply mappings and transformations
                var transformedJson = ApplyMappings(sourceElement, source);

                // Deserialize to target type
                return JsonSerializer.Deserialize<TTarget>(transformedJson, options);
            }
            catch (JsonMappingException)
            {
                // Re-throw JsonMappingException without wrapping to avoid double-wrapping
                throw;
            }
            catch (Exception ex)
            {
                throw new JsonMappingException(
                    $"Failed to apply mapping configuration for {typeof(TSource).Name} to {typeof(TTarget).Name}",
                    typeof(TSource),
                    typeof(TTarget),
                    ex);
            }
        }

        private string ApplyMappings(JsonElement sourceElement, TSource source)
        {
            if (sourceElement.ValueKind != JsonValueKind.Object)
                return sourceElement.GetRawText();

            var result = new Dictionary<string, object>();

            // Apply member mappings
            foreach (var prop in sourceElement.EnumerateObject())
            {
                var mapping = _memberMappings.FirstOrDefault(m => m.SourceName == prop.Name);
                var targetName = mapping?.TargetName ?? prop.Name;
                result[targetName] = GetJsonValue(prop.Value);
            }

            // Apply value resolvers
            foreach (var resolver in _valueResolvers)
            {
                try
                {
                    result[resolver.TargetName] = resolver.Resolver(source);
                }
                catch (Exception ex)
                {
                    throw new JsonMappingException(
                        $"Failed to resolve value for property '{resolver.TargetName}'",
                        typeof(TSource),
                        typeof(TTarget),
                        ex);
                }
            }

            return JsonSerializer.Serialize(result);
        }

        private static object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()),
                JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),
                _ => element.GetRawText()
            };
        }

        private static string GetMemberName<T>(Expression<T> expression)
        {
            return expression.Body switch
            {
                MemberExpression memberExpr => memberExpr.Member.Name,
                UnaryExpression { Operand: MemberExpression memberExpr2 } => memberExpr2.Member.Name,
                _ => throw new ArgumentException("Expression must be a member access", nameof(expression))
            };
        }

        private class MemberMapping
        {
            public string TargetName { get; }
            public string SourceName { get; }
            public Func<object, object> Transform { get; }

            public MemberMapping(string targetName, string sourceName, Func<object, object> transform)
            {
                TargetName = targetName;
                SourceName = sourceName;
                Transform = transform;
            }
        }

        private class ValueResolver
        {
            public string TargetName { get; }
            public Func<object, object> Resolver { get; }

            public ValueResolver(string targetName, Func<object, object> resolver)
            {
                TargetName = targetName;
                Resolver = resolver;
            }
        }
    }

    /// <summary>
    /// Exception thrown when JSON mapping operations fail.
    /// </summary>
    public class JsonMappingException : JsonToolkitException
    {
        public Type SourceType { get; }
        public Type TargetType { get; }

        public JsonMappingException(string message, Type sourceType, Type targetType, Exception innerException = null)
            : base(message, innerException)
        {
            SourceType = sourceType;
            TargetType = targetType;
        }
    }
}
