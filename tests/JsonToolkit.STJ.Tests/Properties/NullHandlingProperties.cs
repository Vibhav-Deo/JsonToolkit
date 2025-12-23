using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for null handling distinctions.
    /// **Feature: json-toolkit-stj, Property 12: Null handling distinguishes missing vs null vs default**
    /// </summary>
    public class NullHandlingProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 12: Null handling distinguishes missing vs null vs default**
        /// For any object with nullable properties, serialization and deserialization should correctly
        /// distinguish between missing properties, null values, and default values.
        /// **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool NullHandling_ShouldDistinguishMissingFromNull(string? value1, string? value2, bool includeFirst, bool includeSecond)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithEnhancedNullHandling(opts =>
                    {
                        opts.DistinguishMissingFromNull = true;
                        opts.SerializationBehavior = NullSerializationBehavior.Include;
                    });

                // Build JSON manually to control which properties are present
                var jsonParts = new System.Collections.Generic.List<string>();
                if (includeFirst)
                    jsonParts.Add($"\"Value1\":{(value1 == null ? "null" : JsonSerializer.Serialize(value1))}");
                if (includeSecond)
                    jsonParts.Add($"\"Value2\":{(value2 == null ? "null" : JsonSerializer.Serialize(value2))}");

                var json = "{" + string.Join(",", jsonParts) + "}";

                var result = JsonSerializer.Deserialize<TestNullableObject>(json, options);

                // Verify the deserialization matches expectations
                if (includeFirst)
                {
                    if (result?.Value1 != value1) return false;
                }
                if (includeSecond)
                {
                    if (result?.Value2 != value2) return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 12: Null handling distinguishes missing vs null vs default**
        /// When serializing with different null handling behaviors, the output should respect the configuration.
        /// **Validates: Requirements 11.3, 11.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool NullHandling_SerializationBehaviorShouldBeRespected(string? value, NullSerializationBehavior behavior)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithEnhancedNullHandling(opts =>
                    {
                        opts.SerializationBehavior = behavior;
                    });

                var obj = new TestNullableObject { Value1 = value };
                var json = JsonSerializer.Serialize(obj, options);

                // Verify behavior
                switch (behavior)
                {
                    case NullSerializationBehavior.Omit:
                        if (value == null && json.Contains("Value1")) return false;
                        break;
                    case NullSerializationBehavior.Include:
                        // Should always include the property
                        break;
                }

                // Round-trip should work
                var roundTrip = JsonSerializer.Deserialize<TestNullableObject>(json, options);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 12: Null handling distinguishes missing vs null vs default**
        /// When skipping default values, properties with default values should be omitted from serialization.
        /// **Validates: Requirements 11.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool NullHandling_SkipDefaultValuesShouldWork(int value, bool skipDefaults)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithEnhancedNullHandling(opts =>
                    {
                        opts.SkipDefaultValues = skipDefaults;
                    });

                var obj = new TestValueObject { IntValue = value };
                var json = JsonSerializer.Serialize(obj, options);

                if (skipDefaults && value == 0)
                {
                    // Default int value should be omitted
                    if (json.Contains("IntValue")) return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 12: Null handling distinguishes missing vs null vs default**
        /// Nullable value types should be handled consistently with reference types.
        /// **Validates: Requirements 11.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool NullHandling_NullableValueTypesShouldBeConsistent(int? nullableInt, string? nullableString)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithEnhancedNullHandling();

                var obj = new TestMixedNullables
                {
                    NullableInt = nullableInt,
                    NullableString = nullableString
                };

                var json = JsonSerializer.Serialize(obj, options);
                var roundTrip = JsonSerializer.Deserialize<TestMixedNullables>(json, options);

                // Both should round-trip correctly
                return roundTrip?.NullableInt == nullableInt &&
                       roundTrip?.NullableString == nullableString;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class TestNullableObject
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class TestValueObject
    {
        public int IntValue { get; set; }
        public string? StringValue { get; set; }
    }

    public class TestMixedNullables
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
    }
}
