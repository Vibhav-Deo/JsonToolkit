using System.Linq;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    public class ConfigurationMergeProperties
    {
        public static Arbitrary<string> ValidJsonKey() =>
            Arb.From(Gen.Elements("key", "name", "value", "config", "setting", "data", "test", "prop"));

        public static Arbitrary<string> ValidJsonValue() =>
            Arb.From(Gen.Elements("value1", "value2", "test", "data", "hello", "world", "sample"));

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationMerge_MultipleConfigs_LastWins(string key, string value1, string value2)
        {
            var config1 = $"{{\"{key}\": \"{value1}\"}}";
            var config2 = $"{{\"{key}\": \"{value2}\"}}";

            var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
            var element = JsonDocument.Parse(merged).RootElement;

            return (element.TryGetProperty(key, out var prop) && 
                   prop.GetString() == value2).ToProperty();
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationMerge_NestedObjects_DeepMerges(string key1, string key2, string value1, string value2)
        {
            return (key1 != key2).ToProperty().And(() =>
            {
                var config1 = $"{{\"nested\": {{\"{key1}\": \"{value1}\"}}}}";
                var config2 = $"{{\"nested\": {{\"{key2}\": \"{value2}\"}}}}";

                var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
                var element = JsonDocument.Parse(merged).RootElement;

                return element.TryGetProperty("nested", out var nested) &&
                       nested.TryGetProperty(key1, out var prop1) &&
                       nested.TryGetProperty(key2, out var prop2) &&
                       prop1.GetString() == value1 &&
                       prop2.GetString() == value2;
            }).When(key1 != key2);
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationMerge_EmptyConfig_ReturnsOther(string key, string value)
        {
            var config = $"{{\"{key}\": \"{value}\"}}";
            var empty = "{}";

            var merged1 = ConfigurationMerge.MergeConfigurations(empty, config);
            var merged2 = ConfigurationMerge.MergeConfigurations(config, empty);

            var element1 = JsonDocument.Parse(merged1).RootElement;
            var element2 = JsonDocument.Parse(merged2).RootElement;

            return (element1.TryGetProperty(key, out var prop1) &&
                   prop1.GetString() == value &&
                   element2.TryGetProperty(key, out var prop2) &&
                   prop2.GetString() == value).ToProperty();
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationBuilder_WithFallback_AppliesWhenMissing(string key, string fallbackValue)
        {
            var builder = new ConfigurationBuilder()
                .AddConfiguration("{}")
                .WithFallback(key, fallbackValue);

            var result = builder.Build();
            var element = JsonDocument.Parse(result).RootElement;

            return (element.TryGetProperty(key, out var prop) &&
                   prop.GetString() == fallbackValue).ToProperty();
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationBuilder_WithFallback_DoesNotOverrideExisting(string key, string existingValue, string fallbackValue)
        {
            return (existingValue != fallbackValue).ToProperty().And(() =>
            {
                var config = $"{{\"{key}\": \"{existingValue}\"}}";
                var builder = new ConfigurationBuilder()
                    .AddConfiguration(config)
                    .WithFallback(key, fallbackValue);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key, out var prop) &&
                       prop.GetString() == existingValue;
            }).When(existingValue != fallbackValue);
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationBuilder_MaskSensitiveData_ReplacesValue(string key, string sensitiveValue)
        {
            var config = $"{{\"{key}\": \"{sensitiveValue}\"}}";
            var builder = new ConfigurationBuilder()
                .AddConfiguration(config)
                .MaskSensitiveData(key);

            var result = builder.Build();
            var element = JsonDocument.Parse(result).RootElement;

            return (element.TryGetProperty(key, out var prop) &&
                   prop.GetString() == "***MASKED***").ToProperty();
        }

        [Property]
        public Property ConfigurationMerge_ArraysReplaced_NotMerged(NonEmptyArray<int> array1, NonEmptyArray<int> array2)
        {
            var config1 = JsonSerializer.Serialize(new { items = array1.Get });
            var config2 = JsonSerializer.Serialize(new { items = array2.Get });

            var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
            var element = JsonDocument.Parse(merged).RootElement;

            return (element.TryGetProperty("items", out var items) && 
                   items.ValueKind == JsonValueKind.Array).ToProperty().And(() =>
            {
                var resultArray = items.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                return resultArray.SequenceEqual(array2.Get);
            });
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationMerge_NullOverwrites_ExistingValue(string key)
        {
            var config1 = $"{{\"{key}\": \"value\"}}";
            var config2 = $"{{\"{key}\": null}}";

            var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
            var element = JsonDocument.Parse(merged).RootElement;

            return (element.TryGetProperty(key, out var prop) &&
                   prop.ValueKind == JsonValueKind.Null).ToProperty();
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationBuilder_MultipleConfigs_MergesInOrder(string key1, string key2, string value1, string value2)
        {
            return (key1 != key2).ToProperty().And(() =>
            {
                var config1 = $"{{\"{key1}\": \"{value1}\"}}";
                var config2 = $"{{\"{key2}\": \"{value2}\"}}";

                var builder = new ConfigurationBuilder()
                    .AddConfiguration(config1)
                    .AddConfiguration(config2);

                var result = builder.Build();
                var element = JsonDocument.Parse(result).RootElement;

                return element.TryGetProperty(key1, out var prop1) &&
                       element.TryGetProperty(key2, out var prop2) &&
                       prop1.GetString() == value1 &&
                       prop2.GetString() == value2;
            }).When(key1 != key2);
        }

        [Property(Arbitrary = new[] { typeof(ConfigurationMergeProperties) })]
        public Property ConfigurationMerge_DifferentTypes_SecondWins(string key, int intValue, string strValue)
        {
            var config1 = $"{{\"{key}\": {intValue}}}";
            var config2 = $"{{\"{key}\": \"{strValue}\"}}";

            var merged = ConfigurationMerge.MergeConfigurations(config1, config2);
            var element = JsonDocument.Parse(merged).RootElement;

            return (element.TryGetProperty(key, out var prop) &&
                   prop.ValueKind == JsonValueKind.String &&
                   prop.GetString() == strValue).ToProperty();
        }
    }
}
