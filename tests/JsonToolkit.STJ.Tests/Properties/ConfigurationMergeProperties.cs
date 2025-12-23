using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    public class ConfigurationMergeProperties
    {
        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_MultipleConfigs_LastWins(string key, string value1, string value2)
        {
            return (IsValidJsonPropertyName(key) && value1 != null && value2 != null).ToProperty().And(() =>
            {
                var config1 = $"{{\"{key}\": \"{EscapeJson(value1)}\"}}";
                var config2 = $"{{\"{key}\": \"{EscapeJson(value2)}\"}}";

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                return element.TryGetProperty(key, out var prop) && 
                       prop.GetString() == value2;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_NestedObjects_DeepMerges(string key1, string key2, string value1, string value2)
        {
            return (IsValidJsonPropertyName(key1) && IsValidJsonPropertyName(key2) && 
                    key1 != key2 && value1 != null && value2 != null).ToProperty().And(() =>
            {
                var config1 = $"{{\"nested\": {{\"{key1}\": \"{EscapeJson(value1)}\"}}}}";
                var config2 = $"{{\"nested\": {{\"{key2}\": \"{EscapeJson(value2)}\"}}}}";

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                return element.TryGetProperty("nested", out var nested) &&
                       nested.TryGetProperty(key1, out var prop1) &&
                       nested.TryGetProperty(key2, out var prop2) &&
                       prop1.GetString() == value1 &&
                       prop2.GetString() == value2;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_EmptyConfig_ReturnsOther(string key, string value)
        {
            return (IsValidJsonPropertyName(key) && value != null).ToProperty().And(() =>
            {
                var config = $"{{\"{key}\": \"{EscapeJson(value)}\"}}";
                var empty = "{}";

                var merged1 = ConfigurationMerge.MergeConfigurations(empty, config);
                var merged2 = ConfigurationMerge.MergeConfigurations(config, empty);

                var element1 = JsonDocument.Parse(merged1).RootElement;
                var element2 = JsonDocument.Parse(merged2).RootElement;

                return element1.TryGetProperty(key, out var prop1) &&
                       prop1.GetString() == value &&
                       element2.TryGetProperty(key, out var prop2) &&
                       prop2.GetString() == value;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationBuilder_WithFallback_AppliesWhenMissing(string key, string fallbackValue)
        {
            return (IsValidJsonPropertyName(key) && fallbackValue != null).ToProperty().And(() =>
            {
                var builder = new ConfigurationBuilder()
                    .AddConfiguration("{}")
                    .WithFallback(key, fallbackValue);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.GetString() == fallbackValue;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationBuilder_WithFallback_DoesNotOverrideExisting(string key, string existingValue, string fallbackValue)
        {
            return (IsValidJsonPropertyName(key) && existingValue != null && 
                    fallbackValue != null && existingValue != fallbackValue).ToProperty().And(() =>
            {
                var config = $"{{\"{key}\": \"{EscapeJson(existingValue)}\"}}";
                var builder = new ConfigurationBuilder()
                    .AddConfiguration(config)
                    .WithFallback(key, fallbackValue);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.GetString() == existingValue;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationBuilder_MaskSensitiveData_ReplacesValue(string key, string sensitiveValue)
        {
            return (IsValidJsonPropertyName(key) && sensitiveValue != null).ToProperty().And(() =>
            {
                var config = $"{{\"{key}\": \"{EscapeJson(sensitiveValue)}\"}}";
                var builder = new ConfigurationBuilder()
                    .AddConfiguration(config)
                    .MaskSensitiveData(key);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.GetString() == "***MASKED***";
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_ArraysReplaced_NotMerged(int[] array1, int[] array2)
        {
            return (array1 != null && array2 != null && array1.Length > 0 && array2.Length > 0).ToProperty().And(() =>
            {
                var config1 = JsonSerializer.Serialize(new { items = array1 });
                var config2 = JsonSerializer.Serialize(new { items = array2 });

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                if (!element.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                    return false;

                var resultArray = items.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                return resultArray.SequenceEqual(array2);
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_NullOverwrites_ExistingValue(string key)
        {
            return IsValidJsonPropertyName(key).ToProperty().And(() =>
            {
                var config1 = $"{{\"{key}\": \"value\"}}";
                var config2 = $"{{\"{key}\": null}}";

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.ValueKind == JsonValueKind.Null;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationBuilder_MultipleConfigs_MergesInOrder(string key1, string key2, string value1, string value2)
        {
            return (IsValidJsonPropertyName(key1) && IsValidJsonPropertyName(key2) && 
                    key1 != key2 && value1 != null && value2 != null).ToProperty().And(() =>
            {
                var config1 = $"{{\"{key1}\": \"{EscapeJson(value1)}\"}}";
                var config2 = $"{{\"{key2}\": \"{EscapeJson(value2)}\"}}";

                var builder = new ConfigurationBuilder()
                    .AddConfiguration(config1)
                    .AddConfiguration(config2);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key1, out var prop1) &&
                       element.TryGetProperty(key2, out var prop2) &&
                       prop1.GetString() == value1 &&
                       prop2.GetString() == value2;
            });
        }

        [Property(Arbitrary = new[] { typeof(JsonGenerators) })]
        public Property ConfigurationMerge_DifferentTypes_SecondWins(string key, int intValue, string strValue)
        {
            return (IsValidJsonPropertyName(key) && strValue != null).ToProperty().And(() =>
            {
                var config1 = $"{{\"{key}\": {intValue}}}";
                var config2 = $"{{\"{key}\": \"{EscapeJson(strValue)}\"}}";

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.ValueKind == JsonValueKind.String &&
                       prop.GetString() == strValue;
            });
        }

        private bool IsValidJsonPropertyName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) &&
                   !name.Contains("\"") &&
                   !name.Contains("\\") &&
                   !name.Contains("\n") &&
                   !name.Contains("\r") &&
                   !name.Contains("\t") &&
                   !name.Contains(":") &&
                   name.Length < 100;
        }

        private string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }
    }
}
