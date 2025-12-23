using System;
using System.Collections.Generic;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for polymorphic type resolution.
    /// **Feature: json-toolkit-stj, Property 4: Polymorphic type resolution is deterministic**
    /// </summary>
    public class PolymorphicTypeProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 4: Polymorphic type resolution is deterministic**
        /// For any polymorphic object with a type discriminator, deserialization should resolve to the correct type.
        /// **Validates: Requirements 4.1, 4.2**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool PolymorphicType_ShouldResolveCorrectType(string discriminator, string data)
        {
            try
            {
                var options = new JsonOptionsBuilder()
                    .WithPolymorphicTypes(config =>
                    {
                        config.WithBaseType<PolyBase>()
                              .WithTypeProperty("$type")
                              .MapType<PolyDerived1>("type1")
                              .MapType<PolyDerived2>("type2");
                    })
                    .Build();

                // Only test with valid discriminators
                if (discriminator != "type1" && discriminator != "type2")
                    return true;

                var json = $"{{\"$type\":\"{discriminator}\",\"Data\":\"{data}\"}}";
                var result = JsonSerializer.Deserialize<PolyBase>(json, options);

                if (result == null) return false;

                // Verify correct type was resolved
                if (discriminator == "type1" && result is not PolyDerived1) return false;
                if (discriminator == "type2" && result is not PolyDerived2) return false;

                // Verify data was preserved
                return result.Data == data;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 4: Polymorphic type resolution is deterministic**
        /// Arrays of polymorphic objects should handle each element independently.
        /// **Validates: Requirements 4.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool PolymorphicType_ArraysShouldHandleElementsIndependently(bool firstIsType1, bool secondIsType1)
        {
            try
            {
                var options = new JsonOptionsBuilder()
                    .WithPolymorphicTypes(config =>
                    {
                        config.WithBaseType<PolyBase>()
                              .WithTypeProperty("$type")
                              .MapType<PolyDerived1>("type1")
                              .MapType<PolyDerived2>("type2");
                    })
                    .Build();

                var type1 = firstIsType1 ? "type1" : "type2";
                var type2 = secondIsType1 ? "type1" : "type2";
                
                var json = $"[{{\"$type\":\"{type1}\",\"Data\":\"first\"}},{{\"$type\":\"{type2}\",\"Data\":\"second\"}}]";
                var result = JsonSerializer.Deserialize<PolyBase[]>(json, options);

                if (result == null || result.Length != 2) return false;

                // Verify each element has correct type
                if (firstIsType1 && result[0] is not PolyDerived1) return false;
                if (!firstIsType1 && result[0] is not PolyDerived2) return false;
                if (secondIsType1 && result[1] is not PolyDerived1) return false;
                if (!secondIsType1 && result[1] is not PolyDerived2) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 4: Polymorphic type resolution is deterministic**
        /// When type discriminator is missing and fallback is configured, should use fallback type.
        /// **Validates: Requirements 4.5**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool PolymorphicType_MissingDiscriminatorShouldUseFallback(string data)
        {
            try
            {
                if (data == null || data.Length == 0) return true;

                var options = new JsonOptionsBuilder()
                    .WithPolymorphicTypes(config =>
                    {
                        config.WithBaseType<PolyBase>()
                              .WithTypeProperty("$type")
                              .WithFallbackType<PolyDerived1>()
                              .MapType<PolyDerived1>("type1")
                              .MapType<PolyDerived2>("type2");
                    })
                    .Build();

                // Use proper serialization to avoid escaping issues
                var obj = new { Data = data };
                var json = JsonSerializer.Serialize(obj);
                var result = JsonSerializer.Deserialize<PolyBase>(json, options);

                // Should use fallback type
                return result is PolyDerived1 && result.Data == data;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 4: Polymorphic type resolution is deterministic**
        /// Round-trip serialization should preserve type information.
        /// **Validates: Requirements 4.1, 4.2**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool PolymorphicType_RoundTripShouldPreserveType(string data, bool useType1)
        {
            try
            {
                var options = new JsonOptionsBuilder()
                    .WithPolymorphicTypes(config =>
                    {
                        config.WithBaseType<PolyBase>()
                              .WithTypeProperty("$type")
                              .MapType<PolyDerived1>("type1")
                              .MapType<PolyDerived2>("type2");
                    })
                    .Build();

                PolyBase original = useType1 
                    ? new PolyDerived1 { Data = data }
                    : new PolyDerived2 { Data = data };

                var json = JsonSerializer.Serialize(original, options);
                var roundTrip = JsonSerializer.Deserialize<PolyBase>(json, options);

                if (roundTrip == null) return false;
                if (roundTrip.GetType() != original.GetType()) return false;
                if (roundTrip.Data != original.Data) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public abstract class PolyBase
    {
        public string? Data { get; set; }
    }

    public class PolyDerived1 : PolyBase
    {
    }

    public class PolyDerived2 : PolyBase
    {
    }
}
