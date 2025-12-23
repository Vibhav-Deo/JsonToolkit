using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    public class JsonMapper
    {
        private readonly Dictionary<string, string> _propertyMappings = new Dictionary<string, string>();
        private readonly Dictionary<string, Func<object, object>> _transformations = new Dictionary<string, Func<object, object>>();
        private JsonSerializerOptions _options = new JsonSerializerOptions();

        public JsonMapper MapProperty(string sourceName, string targetName)
        {
            _propertyMappings[sourceName] = targetName;
            return this;
        }

        public JsonMapper Transform(string propertyName, Func<object, object> transformation)
        {
            _transformations[propertyName] = transformation;
            return this;
        }

        public JsonMapper WithOptions(JsonSerializerOptions options)
        {
            _options = options;
            return this;
        }

        public TTarget Map<TSource, TTarget>(TSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var json = JsonSerializer.Serialize(source, _options);
            var element = JsonDocument.Parse(json).RootElement;

            var transformed = ApplyMappings(element);
            
            return JsonSerializer.Deserialize<TTarget>(transformed, _options);
        }

        private string ApplyMappings(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return element.GetRawText();

            var result = new Dictionary<string, object>();

            foreach (var prop in element.EnumerateObject())
            {
                var targetName = _propertyMappings.ContainsKey(prop.Name) 
                    ? _propertyMappings[prop.Name] 
                    : prop.Name;

                var value = GetValue(prop.Value);

                if (_transformations.ContainsKey(prop.Name))
                {
                    value = _transformations[prop.Name](value);
                }

                result[targetName] = value;
            }

            return JsonSerializer.Serialize(result);
        }

        private object GetValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return JsonSerializer.Deserialize<object>(element.GetRawText());
                default:
                    return element.GetRawText();
            }
        }
    }

    public class MappingConfiguration<TSource, TTarget>
    {
        private readonly JsonMapper _mapper = new JsonMapper();

        public MappingConfiguration<TSource, TTarget> ForMember(string sourceName, string targetName)
        {
            _mapper.MapProperty(sourceName, targetName);
            return this;
        }

        public MappingConfiguration<TSource, TTarget> WithTransform(string propertyName, Func<object, object> transformation)
        {
            _mapper.Transform(propertyName, transformation);
            return this;
        }

        public TTarget Map(TSource source)
        {
            return _mapper.Map<TSource, TTarget>(source);
        }
    }
}
