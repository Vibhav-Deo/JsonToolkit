using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    public static class ConfigurationMerge
    {
        public static string MergeConfigurations(params string[] jsonConfigs)
        {
            if (jsonConfigs == null || jsonConfigs.Length == 0)
                return "{}";

            var result = JsonDocument.Parse(jsonConfigs[0]).RootElement.Clone();
            
            for (int i = 1; i < jsonConfigs.Length; i++)
            {
                var next = JsonDocument.Parse(jsonConfigs[i]).RootElement;
                result = DeepMergeElements(result, next);
            }

            return JsonSerializer.Serialize(result);
        }

        public static JsonElement MergeConfigurations(params JsonElement[] configs)
        {
            if (configs == null || configs.Length == 0)
                return JsonDocument.Parse("{}").RootElement;

            var result = configs[0].Clone();
            
            for (int i = 1; i < configs.Length; i++)
            {
                result = DeepMergeElements(result, configs[i]);
            }

            return result;
        }

        private static JsonElement DeepMergeElements(JsonElement target, JsonElement source)
        {
            if (source.ValueKind != JsonValueKind.Object || target.ValueKind != JsonValueKind.Object)
                return source.Clone();

            var merged = new Dictionary<string, JsonElement>();

            foreach (var prop in target.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            foreach (var prop in source.EnumerateObject())
            {
                if (merged.ContainsKey(prop.Name) && 
                    merged[prop.Name].ValueKind == JsonValueKind.Object && 
                    prop.Value.ValueKind == JsonValueKind.Object)
                {
                    merged[prop.Name] = DeepMergeElements(merged[prop.Name], prop.Value);
                }
                else
                {
                    merged[prop.Name] = prop.Value.Clone();
                }
            }

            return JsonSerializer.SerializeToElement(merged);
        }
    }
}
