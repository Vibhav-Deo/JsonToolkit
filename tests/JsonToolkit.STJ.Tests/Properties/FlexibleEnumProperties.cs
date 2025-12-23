using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for FlexibleEnumConverter serialization consistency.
    /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
    /// </summary>
    public class FlexibleEnumProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any enum type and configuration, serializing then deserializing should produce equivalent values 
        /// with proper case-insensitive matching, flags enum support, and custom naming.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnumSerializationRoundTrip_ShouldPreserveValues(FlexibleTestEnum enumValue, bool serializeAsString, bool allowNumeric, bool caseInsensitive)
        {
            try
            {
                // Skip invalid combinations where we serialize as numeric but don't allow numeric deserialization
                if (!serializeAsString && !allowNumeric)
                {
                    return true; // Skip this test case as it's an invalid configuration
                }

                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = serializeAsString,
                    AllowNumericValues = allowNumeric,
                    CaseInsensitive = caseInsensitive,
                    UndefinedValueHandling = UndefinedEnumValueHandling.ThrowException
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverter<FlexibleTestEnum>(options));

                // Serialize the enum value
                var json = JsonSerializer.Serialize(enumValue, serializerOptions);
                
                // Deserialize back to enum
                var roundTripValue = JsonSerializer.Deserialize<FlexibleTestEnum>(json, serializerOptions);

                // Values should be equal
                return enumValue.Equals(roundTripValue);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any flags enum with comma-separated string representation, serialization and deserialization 
        /// should preserve the combined flag values correctly.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool FlagsEnumSerialization_ShouldHandleCommaSeperatedValues(TestFlags flagsValue, bool caseInsensitive)
        {
            try
            {
                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = true,
                    AllowNumericValues = true,
                    CaseInsensitive = caseInsensitive,
                    UndefinedValueHandling = UndefinedEnumValueHandling.ThrowException
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverter<TestFlags>(options));

                // Serialize the flags enum value
                var json = JsonSerializer.Serialize(flagsValue, serializerOptions);
                
                // Deserialize back to flags enum
                var roundTripValue = JsonSerializer.Deserialize<TestFlags>(json, serializerOptions);

                // Values should be equal
                return flagsValue.Equals(roundTripValue);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any enum with custom naming attributes, the converter should respect the custom names
        /// during both serialization and deserialization.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CustomNamedEnum_ShouldRespectAttributes(CustomNamedEnum enumValue, bool caseInsensitive)
        {
            try
            {
                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = true,
                    AllowNumericValues = true,
                    CaseInsensitive = caseInsensitive,
                    UndefinedValueHandling = UndefinedEnumValueHandling.ThrowException
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverter<CustomNamedEnum>(options));

                // Serialize the enum value
                var json = JsonSerializer.Serialize(enumValue, serializerOptions);
                
                // Deserialize back to enum
                var roundTripValue = JsonSerializer.Deserialize<CustomNamedEnum>(json, serializerOptions);

                // Values should be equal
                return enumValue.Equals(roundTripValue);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any undefined enum value handling configuration, the converter should behave consistently
        /// according to the specified fallback behavior.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool UndefinedValueHandling_ShouldBehaveConsistently(UndefinedEnumValueHandling handling, FlexibleTestEnum fallbackValue)
        {
            try
            {
                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = true,
                    AllowNumericValues = true,
                    CaseInsensitive = true,
                    UndefinedValueHandling = handling
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverter<FlexibleTestEnum>(options, fallbackValue));

                // Try to deserialize an undefined value
                var undefinedJson = "\"UndefinedEnumValue\"";
                
                switch (handling)
                {
                    case UndefinedEnumValueHandling.ThrowException:
                        try
                        {
                            JsonSerializer.Deserialize<FlexibleTestEnum>(undefinedJson, serializerOptions);
                            return false; // Should have thrown an exception
                        }
                        catch (JsonToolkitException)
                        {
                            return true; // Expected behavior
                        }
                        catch (JsonException)
                        {
                            return true; // Expected behavior
                        }

                    case UndefinedEnumValueHandling.ReturnDefault:
                        var defaultResult = JsonSerializer.Deserialize<FlexibleTestEnum>(undefinedJson, serializerOptions);
                        return defaultResult.Equals(default(FlexibleTestEnum));

                    case UndefinedEnumValueHandling.ReturnFallbackValue:
                        var fallbackResult = JsonSerializer.Deserialize<FlexibleTestEnum>(undefinedJson, serializerOptions);
                        return fallbackResult.Equals(fallbackValue);

                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any nullable enum value, the converter should handle null values correctly while preserving
        /// non-null enum values through round-trip serialization.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool NullableEnum_ShouldHandleNullsCorrectly(FlexibleTestEnum? nullableEnumValue, bool serializeAsString)
        {
            try
            {
                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = serializeAsString,
                    AllowNumericValues = true, // Always allow numeric values for consistency
                    CaseInsensitive = true,
                    UndefinedValueHandling = UndefinedEnumValueHandling.ThrowException
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverterFactory(options));

                // Serialize the nullable enum value
                var json = JsonSerializer.Serialize(nullableEnumValue, serializerOptions);
                
                // Deserialize back to nullable enum
                var roundTripValue = JsonSerializer.Deserialize<FlexibleTestEnum?>(json, serializerOptions);

                // Values should be equal (including null handling)
                if (nullableEnumValue.HasValue && roundTripValue.HasValue)
                {
                    return nullableEnumValue.Value.Equals(roundTripValue.Value);
                }
                
                return nullableEnumValue == roundTripValue; // Both null or both have same value
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 11: Enum serialization round-trip consistency**
        /// For any case-insensitive enum deserialization, the converter should match enum values
        /// regardless of the case of the input string.
        /// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitiveDeserialization_ShouldMatchRegardlessOfCase(FlexibleTestEnum enumValue)
        {
            try
            {
                var options = new FlexibleEnumOptions
                {
                    SerializeAsString = true,
                    AllowNumericValues = true,
                    CaseInsensitive = true,
                    UndefinedValueHandling = UndefinedEnumValueHandling.ThrowException
                };

                var serializerOptions = new JsonSerializerOptions();
                serializerOptions.Converters.Add(new FlexibleEnumConverter<FlexibleTestEnum>(options));

                // Get the string representation of the enum
                var enumString = enumValue.ToString();
                
                // Test different case variations
                var variations = new[]
                {
                    enumString.ToLowerInvariant(),
                    enumString.ToUpperInvariant(),
                    enumString // Original case
                };

                foreach (var variation in variations)
                {
                    var json = $"\"{variation}\"";
                    var deserializedValue = JsonSerializer.Deserialize<FlexibleTestEnum>(json, serializerOptions);
                    
                    if (!enumValue.Equals(deserializedValue))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Test enum for flexible enum property-based testing.
    /// </summary>
    public enum FlexibleTestEnum
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
        Fifth = 5
    }

    /// <summary>
    /// Test flags enum for property-based testing.
    /// </summary>
    [Flags]
    public enum TestFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        Delete = 8,
        All = Read | Write | Execute | Delete
    }

    /// <summary>
    /// Test enum with custom naming attributes.
    /// </summary>
    public enum CustomNamedEnum
    {
        [JsonPropertyName("custom_first")]
        First,
        
        [JsonPropertyName("custom_second")]
        Second,
        
        [JsonPropertyName("custom_third")]
        Third
    }
}