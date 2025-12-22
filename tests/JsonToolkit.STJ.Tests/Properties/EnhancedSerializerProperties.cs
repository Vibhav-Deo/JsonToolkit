using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FsCheck.Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for enhanced serializer compatibility.
    /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
    /// </summary>
    public class EnhancedSerializerProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// For any simple object, SerializeEnhanced followed by DeserializeEnhanced should produce an equivalent object.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_RoundTrip_ShouldPreserveStructure(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                // Test enhanced serializer round-trip
                var json = JsonSerializerExtensions.SerializeEnhanced(testObj);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(json);
                
                return AreEnhancedObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// For any simple object, enhanced serialization to UTF-8 bytes should preserve structure.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_Utf8Bytes_ShouldPreserveStructure(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                // Test UTF-8 bytes round-trip
                var jsonBytes = JsonSerializerExtensions.SerializeEnhancedToUtf8Bytes(testObj);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(jsonBytes);
                
                return AreEnhancedObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// For any simple object, enhanced serialization with ReadOnlySpan should preserve structure.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_ReadOnlySpan_ShouldPreserveStructure(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                // Test ReadOnlySpan round-trip
                var jsonBytes = JsonSerializerExtensions.SerializeEnhancedToUtf8Bytes(testObj);
                var span = new ReadOnlySpan<byte>(jsonBytes);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(span);
                
                return AreEnhancedObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// For any simple object, async enhanced serialization should preserve structure.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_AsyncStream_ShouldPreserveStructure(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                return Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    
                    // Serialize to stream
                    await JsonSerializerExtensions.SerializeEnhancedAsync(stream, testObj);
                    
                    // Reset stream position
                    stream.Position = 0;
                    
                    // Deserialize from stream
                    var roundTrip = await JsonSerializerExtensions.DeserializeEnhancedAsync<EnhancedTestObject>(stream);
                    
                    return AreEnhancedObjectsEquivalent(testObj, roundTrip);
                }).Result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// For any simple object with custom options, enhanced serialization should preserve structure.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_WithCustomOptions_ShouldPreserveStructure(string stringVal, int intVal, bool boolVal, double doubleVal, bool writeIndented, bool caseInsensitive)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = writeIndented,
                    PropertyNameCaseInsensitive = caseInsensitive
                };
                
                var json = JsonSerializerExtensions.SerializeEnhanced(testObj, options);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(json, options);
                
                return AreEnhancedObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Enhanced serializer should be compatible with standard JsonSerializer for equivalent operations.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_CompatibilityWithStandard_ShouldProduceEquivalentResults(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            // Skip special double values that JSON doesn't support
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                return true;
                
            try
            {
                var testObj = new EnhancedTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal,
                    DoubleValue = doubleVal
                };
                
                // Serialize with both enhanced and standard serializers
                var enhancedJson = JsonSerializerExtensions.SerializeEnhanced(testObj);
                var standardJson = JsonSerializer.Serialize(testObj);
                
                // Both should deserialize to equivalent objects
                var enhancedRoundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(enhancedJson);
                var standardRoundTrip = JsonSerializer.Deserialize<EnhancedTestObject>(standardJson);
                
                // Cross-compatibility: enhanced serializer should deserialize standard JSON and vice versa
                var crossRoundTrip1 = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(standardJson);
                var crossRoundTrip2 = JsonSerializer.Deserialize<EnhancedTestObject>(enhancedJson);
                
                return AreEnhancedObjectsEquivalent(testObj, enhancedRoundTrip) &&
                       AreEnhancedObjectsEquivalent(testObj, standardRoundTrip) &&
                       AreEnhancedObjectsEquivalent(testObj, crossRoundTrip1) &&
                       AreEnhancedObjectsEquivalent(testObj, crossRoundTrip2);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Enhanced serializer should handle null values consistently with structure preservation.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_NullHandling_ShouldPreserveStructure()
        {
            try
            {
                EnhancedTestObject? nullObj = null;
                
                // Enhanced serializer should handle null
                var json = JsonSerializerExtensions.SerializeEnhanced(nullObj);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject?>(json);
                
                // Test object with null properties
                var objWithNulls = new EnhancedTestObject 
                { 
                    StringValue = null, 
                    IntValue = 0, 
                    BoolValue = false,
                    DoubleValue = 0.0
                };
                
                var jsonWithNulls = JsonSerializerExtensions.SerializeEnhanced(objWithNulls);
                var roundTripWithNulls = JsonSerializerExtensions.DeserializeEnhanced<EnhancedTestObject>(jsonWithNulls);
                
                return json == "null" && 
                       roundTrip == null && 
                       AreEnhancedObjectsEquivalent(objWithNulls, roundTripWithNulls);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Enhanced serializer should handle nested objects while preserving structure.
        /// **Validates: Requirements 5.1, 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool EnhancedSerializer_NestedObjects_ShouldPreserveStructure(string outerString, int outerInt, string innerString, bool innerBool)
        {
            try
            {
                var testObj = new NestedTestObject 
                { 
                    OuterString = outerString,
                    OuterInt = outerInt,
                    Inner = new EnhancedTestObject
                    {
                        StringValue = innerString,
                        BoolValue = innerBool,
                        IntValue = 42,
                        DoubleValue = 3.14
                    }
                };
                
                var json = JsonSerializerExtensions.SerializeEnhanced(testObj);
                var roundTrip = JsonSerializerExtensions.DeserializeEnhanced<NestedTestObject>(json);
                
                return AreNestedObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool AreEnhancedObjectsEquivalent(EnhancedTestObject? obj1, EnhancedTestObject? obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            
            return obj1.StringValue == obj2.StringValue &&
                   obj1.IntValue == obj2.IntValue &&
                   obj1.BoolValue == obj2.BoolValue &&
                   Math.Abs(obj1.DoubleValue - obj2.DoubleValue) < 1e-10; // Handle floating point precision
        }

        private static bool AreNestedObjectsEquivalent(NestedTestObject? obj1, NestedTestObject? obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            
            return obj1.OuterString == obj2.OuterString &&
                   obj1.OuterInt == obj2.OuterInt &&
                   AreEnhancedObjectsEquivalent(obj1.Inner, obj2.Inner);
        }
    }

    /// <summary>
    /// Enhanced test object for property-based testing of enhanced serializer.
    /// </summary>
    public class EnhancedTestObject
    {
        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public bool BoolValue { get; set; }
        public double DoubleValue { get; set; }
    }

    /// <summary>
    /// Nested test object for testing structure preservation.
    /// </summary>
    public class NestedTestObject
    {
        public string? OuterString { get; set; }
        public int OuterInt { get; set; }
        public EnhancedTestObject? Inner { get; set; }
    }
}