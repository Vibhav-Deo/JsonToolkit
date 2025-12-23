using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for JsonOptionsBuilder configuration consistency.
    /// **Feature: json-toolkit-stj, Property 2: Configuration consistency**
    /// </summary>
    public class ConfigurationBuilderProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 2: Configuration consistency**
        /// For any configuration built using JsonOptionsBuilder, the resulting JsonSerializerOptions
        /// should be valid and consistent with the specified configuration settings.
        /// **Validates: Requirements 6.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConfigurationConsistency_ShouldProduceValidOptions(bool caseInsensitive, bool flexibleEnums, bool writeIndented)
        {
            try
            {
                var builder = new JsonOptionsBuilder();
                
                // Apply random configuration settings
                if (caseInsensitive)
                    builder.WithCaseInsensitiveProperties();
                    
                if (flexibleEnums)
                    builder.WithFlexibleEnums();
                    
                if (writeIndented)
                    builder.WithIndentation(true);

                // Build the options
                var options = builder.Build();
                
                // Verify the options are valid by attempting to use them
                var testObject = new { Name = "Test", Value = 42, Status = TestEnum.Active };
                var json = JsonSerializer.Serialize(testObject, options);
                var roundTrip = JsonSerializer.Deserialize<JsonElement>(json, options);
                
                // Verify configuration was applied correctly
                var configurationApplied = VerifyConfigurationSettings(options, caseInsensitive, flexibleEnums, writeIndented);
                
                return !string.IsNullOrEmpty(json) && 
                       roundTrip.ValueKind != JsonValueKind.Undefined &&
                       configurationApplied;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 2: Configuration consistency**
        /// For any JsonOptionsBuilder configuration with extension methods, the resulting options
        /// should maintain consistency and not conflict with each other.
        /// **Validates: Requirements 6.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConfigurationConsistency_ExtensionMethodsShouldNotConflict(bool enableToolkit, bool newtonsoftCompat, bool betterNulls)
        {
            try
            {
                var options = new JsonSerializerOptions();
                
                // Apply extension methods in random order
                if (enableToolkit)
                    options.EnableJsonToolkit();
                    
                if (newtonsoftCompat)
                    options.WithNewtonsoftCompatibility();
                    
                if (betterNulls)
                    options.WithBetterNulls();

                // Validate the final configuration
                options.Validate();
                
                // Test that the options work correctly
                var testObject = new { 
                    Name = "Test", 
                    Value = (int?)42, 
                    NullValue = (string?)null,
                    Status = TestEnum.Active 
                };
                
                var json = JsonSerializer.Serialize(testObject, options);
                var roundTrip = JsonSerializer.Deserialize<JsonElement>(json, options);
                
                return !string.IsNullOrEmpty(json) && 
                       roundTrip.ValueKind != JsonValueKind.Undefined;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 2: Configuration consistency**
        /// For any JsonOptionsBuilder with custom converters, the configuration should handle
        /// converter registration correctly and detect conflicts appropriately.
        /// **Validates: Requirements 6.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConfigurationConsistency_ConverterRegistrationShouldBeValid(PositiveInt converterCount)
        {
            try
            {
                var builder = new JsonOptionsBuilder();
                var actualConverterCount = Math.Min(converterCount.Get, 5); // Limit to reasonable number
                
                // Add multiple converters of different types
                for (int i = 0; i < actualConverterCount; i++)
                {
                    // Add different types of converters to avoid conflicts
                    switch (i % 3)
                    {
                        case 0:
                            builder.WithConverter(new JsonStringEnumConverter());
                            break;
                        case 1:
                            // Skip duplicate enum converters to avoid conflicts
                            continue;
                        case 2:
                            // Add a custom converter for DateTime
                            builder.WithConverter(new TestDateTimeConverter());
                            break;
                    }
                }
                
                var options = builder.Build();
                
                // Verify the options work with the registered converters
                var testObject = new { 
                    Date = DateTime.Now,
                    Status = TestEnum.Active
                };
                
                var json = JsonSerializer.Serialize(testObject, options);
                var roundTrip = JsonSerializer.Deserialize<JsonElement>(json, options);
                
                return !string.IsNullOrEmpty(json) && 
                       roundTrip.ValueKind != JsonValueKind.Undefined &&
                       options.Converters.Count > 0;
            }
            catch (JsonToolkitException ex) when (ex.Message.Contains("Multiple converters"))
            {
                // This is expected behavior for conflicting converters
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 2: Configuration consistency**
        /// For any JsonOptionsBuilder configuration, cloning the resulting options should produce
        /// an equivalent configuration that works identically.
        /// **Validates: Requirements 6.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConfigurationConsistency_CloningShouldPreserveConfiguration(bool caseInsensitive, bool writeIndented)
        {
            try
            {
                var builder = new JsonOptionsBuilder();
                
                if (caseInsensitive)
                    builder.WithCaseInsensitiveProperties();
                    
                if (writeIndented)
                    builder.WithIndentation(true);

                var originalOptions = builder.Build();
                var clonedOptions = originalOptions.Clone();
                
                // Test that both options produce identical results
                var testObject = new { Name = "Test", Value = 42 };
                
                var originalJson = JsonSerializer.Serialize(testObject, originalOptions);
                var clonedJson = JsonSerializer.Serialize(testObject, clonedOptions);
                
                // The JSON should be functionally equivalent (though formatting might differ)
                var originalElement = JsonSerializer.Deserialize<JsonElement>(originalJson);
                var clonedElement = JsonSerializer.Deserialize<JsonElement>(clonedJson);
                
                return CompareJsonElements(originalElement, clonedElement);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool VerifyConfigurationSettings(JsonSerializerOptions options, bool caseInsensitive, bool flexibleEnums, bool writeIndented)
        {
            try
            {
                // Verify case sensitivity setting
                if (caseInsensitive && !options.PropertyNameCaseInsensitive)
                    return false;
                    
                // Verify indentation setting
                if (writeIndented && !options.WriteIndented)
                    return false;
                    
                // Verify enum converter is present if flexible enums were requested
                if (flexibleEnums)
                {
                    bool hasEnumConverter = false;
                    foreach (var converter in options.Converters)
                    {
                        var converterTypeName = converter.GetType().Name;
                        if (converter is JsonStringEnumConverter || 
                            converterTypeName.Contains("FlexibleEnum") ||
                            converter is JsonConverterFactory factory && factory.GetType().Name.Contains("FlexibleEnum"))
                        {
                            hasEnumConverter = true;
                            break;
                        }
                    }
                    if (!hasEnumConverter)
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CompareJsonElements(JsonElement element1, JsonElement element2)
        {
            if (element1.ValueKind != element2.ValueKind)
                return false;

            switch (element1.ValueKind)
            {
                case JsonValueKind.Object:
                    var props1 = new Dictionary<string, JsonElement>();
                    var props2 = new Dictionary<string, JsonElement>();
                    
                    foreach (var prop in element1.EnumerateObject())
                        props1[prop.Name] = prop.Value;
                        
                    foreach (var prop in element2.EnumerateObject())
                        props2[prop.Name] = prop.Value;
                        
                    if (props1.Count != props2.Count)
                        return false;
                        
                    foreach (var kvp in props1)
                    {
                        if (!props2.TryGetValue(kvp.Key, out var value2))
                            return false;
                        if (!CompareJsonElements(kvp.Value, value2))
                            return false;
                    }
                    return true;

                case JsonValueKind.Array:
                    var array1 = new List<JsonElement>();
                    var array2 = new List<JsonElement>();
                    
                    foreach (var item in element1.EnumerateArray())
                        array1.Add(item);
                        
                    foreach (var item in element2.EnumerateArray())
                        array2.Add(item);
                    
                    if (array1.Count != array2.Count)
                        return false;
                        
                    for (int i = 0; i < array1.Count; i++)
                    {
                        if (!CompareJsonElements(array1[i], array2[i]))
                            return false;
                    }
                    return true;

                case JsonValueKind.String:
                    return element1.GetString() == element2.GetString();
                    
                case JsonValueKind.Number:
                    return element1.GetDecimal() == element2.GetDecimal();
                    
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element1.GetBoolean() == element2.GetBoolean();
                    
                case JsonValueKind.Null:
                    return true;
                    
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Test enum for property-based testing.
    /// </summary>
    public enum TestEnum
    {
        Active,
        Inactive,
        Pending
    }

    /// <summary>
    /// Test DateTime converter for property-based testing.
    /// </summary>
    public class TestDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString() ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}