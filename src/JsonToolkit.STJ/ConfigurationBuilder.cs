namespace JsonToolkit.STJ;

public class ConfigurationBuilder
{
        private readonly List<string> _configSources = [];
        private readonly Dictionary<string, string> _fallbacks = [];
        private readonly HashSet<string> _maskedPaths = [];

        public ConfigurationBuilder AddConfiguration(string json)
        {
            _configSources.Add(json);
            return this;
        }

        public ConfigurationBuilder AddEnvironmentOverride(string baseConfig, string environment)
        {
            _configSources.Add(baseConfig);
            if (!string.IsNullOrEmpty(environment))
            {
                var envConfig = ApplyEnvironmentOverride(baseConfig, environment);
                if (!string.IsNullOrEmpty(envConfig))
                    _configSources.Add(envConfig!);
            }
            return this;
        }

        public ConfigurationBuilder WithFallback(string path, string defaultValue)
        {
            _fallbacks[path] = defaultValue;
            return this;
        }

        public ConfigurationBuilder MaskSensitiveData(string path)
        {
            _maskedPaths.Add(path);
            return this;
        }

        public string Build()
        {
            if (_configSources.Count == 0)
                return "{}";

            var merged = ConfigurationMerge.MergeConfigurations(_configSources.ToArray());
            var element = JsonDocument.Parse(merged).RootElement;

            element = ApplyFallbacks(element);
            element = MaskSensitiveValues(element);

            return JsonSerializer.Serialize(element);
        }

        public JsonElement BuildElement()
        {
            return JsonDocument.Parse(Build()).RootElement;
        }

        private JsonElement ApplyFallbacks(JsonElement element)
        {
            if (_fallbacks.Count == 0)
                return element;

            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText()) 
                ?? throw new JsonException("Failed to deserialize configuration element");
            
            foreach (var fallback in _fallbacks)
            {
                if (!HasPath(element, fallback.Key))
                {
                    SetPath(dict, fallback.Key, fallback.Value);
                }
            }

            return JsonSerializer.SerializeToElement(dict);
        }

        private JsonElement MaskSensitiveValues(JsonElement element)
        {
            if (_maskedPaths.Count == 0)
                return element;

            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText())
                ?? throw new JsonException("Failed to deserialize configuration element");
            
            foreach (var path in _maskedPaths)
            {
                if (HasPath(element, path))
                {
                    SetPath(dict, path, "***MASKED***");
                }
            }

            return JsonSerializer.SerializeToElement(dict);
        }

        private bool HasPath(JsonElement element, string path)
        {
            var parts = path.Split(':');
            var current = element;

            foreach (var part in parts)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                    return false;
            }

            return true;
        }

        private void SetPath(Dictionary<string, object> dict, string path, string value)
        {
            var parts = path.Split(':');
            var current = dict as object;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (current is Dictionary<string, object> d)
                {
                    if (!d.ContainsKey(parts[i]))
                        d[parts[i]] = new Dictionary<string, object>();
                    current = d[parts[i]];
                }
            }

            if (current is Dictionary<string, object> final)
            {
                final[parts[parts.Length - 1]] = value;
            }
        }

        private static string? ApplyEnvironmentOverride(string baseConfig, string environment)
        {
            // Placeholder for environment-specific logic
            // In real scenarios, this would load environment-specific files
            return null;
        }
    }
