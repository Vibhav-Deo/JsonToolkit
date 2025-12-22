using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ.Extensions;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for extension method round-trip consistency.
    /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
    /// </summary>
    public class ExtensionMethodProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object, using ToJson() followed by FromJson<T>() should produce an equivalent object.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_SimpleObjects_ShouldPreserveEquality(string stringVal, int intVal, bool boolVal)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                // Test basic round-trip with ToJson/FromJson
                var json = testObj.ToJson();
                var roundTrip = json.FromJson<SimpleTestObject>();
                
                return AreSimpleObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object, using ToJsonBytes() followed by deserialization should produce an equivalent object.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_ToJsonBytes_ShouldPreserveEquality(string stringVal, int intVal, bool boolVal)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                // Test byte array round-trip
                var jsonBytes = testObj.ToJsonBytes();
                var json = Encoding.UTF8.GetString(jsonBytes);
                var roundTrip = json.FromJson<SimpleTestObject>();
                
                return AreSimpleObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object, using DeepClone() should produce an equivalent but separate object instance.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_DeepClone_ShouldPreserveEqualityButCreateNewInstance(string stringVal, int intVal, bool boolVal)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                var cloned = testObj.DeepClone();
                
                // Should be equivalent but not the same reference
                var areEquivalent = AreSimpleObjectsEquivalent(testObj, cloned);
                var areDifferentReferences = !ReferenceEquals(testObj, cloned);
                
                return areEquivalent && areDifferentReferences;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object, async stream serialization/deserialization should produce equivalent results.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_AsyncStream_ShouldPreserveEquality(string stringVal, int intVal, bool boolVal)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                return Task.Run(async () =>
                {
                    using var stream = new MemoryStream();
                    
                    // Serialize to stream
                    await testObj.ToJsonAsync(stream);
                    
                    // Reset stream position
                    stream.Position = 0;
                    
                    // Deserialize from stream
                    var roundTrip = await stream.FromJsonAsync<SimpleTestObject>();
                    
                    return AreSimpleObjectsEquivalent(testObj, roundTrip);
                }).Result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object with custom JsonSerializerOptions, round-trip should preserve equality.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_WithCustomOptions_ShouldPreserveEquality(string stringVal, int intVal, bool boolVal, bool writeIndented, bool caseInsensitive)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = writeIndented,
                    PropertyNameCaseInsensitive = caseInsensitive
                };
                
                var json = testObj.ToJson(options);
                var roundTrip = json.FromJson<SimpleTestObject>(options);
                
                return AreSimpleObjectsEquivalent(testObj, roundTrip);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For any simple object, round-trip through different extension method combinations should be consistent.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_DifferentMethods_ShouldProduceConsistentResults(string stringVal, int intVal, bool boolVal)
        {
            try
            {
                var testObj = new SimpleTestObject 
                { 
                    StringValue = stringVal, 
                    IntValue = intVal, 
                    BoolValue = boolVal 
                };
                
                // Test multiple round-trip paths
                var json1 = testObj.ToJson();
                var roundTrip1 = json1.FromJson<SimpleTestObject>();
                
                var jsonBytes = testObj.ToJsonBytes();
                var json2 = Encoding.UTF8.GetString(jsonBytes);
                var roundTrip2 = json2.FromJson<SimpleTestObject>();
                
                var cloned = testObj.DeepClone();
                
                // All methods should produce equivalent results
                return AreSimpleObjectsEquivalent(testObj, roundTrip1) &&
                       AreSimpleObjectsEquivalent(testObj, roundTrip2) &&
                       AreSimpleObjectsEquivalent(testObj, cloned) &&
                       AreSimpleObjectsEquivalent(roundTrip1, roundTrip2) &&
                       AreSimpleObjectsEquivalent(roundTrip1, cloned);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 15: Extension method round-trip consistency**
        /// For null objects, extension methods should handle null values appropriately.
        /// **Validates: Requirements 14.1, 14.2, 14.3, 14.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ExtensionMethodRoundTrip_NullHandling_ShouldBeConsistent()
        {
            try
            {
                SimpleTestObject? nullObj = null;
                
                // ToJson should handle null
                var json = nullObj.ToJson();
                var roundTrip = json.FromJson<SimpleTestObject?>();
                
                // DeepClone should handle null
                var cloned = nullObj.DeepClone();
                
                return json == "null" && roundTrip == null && cloned == null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool AreSimpleObjectsEquivalent(SimpleTestObject? obj1, SimpleTestObject? obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            
            return obj1.StringValue == obj2.StringValue &&
                   obj1.IntValue == obj2.IntValue &&
                   obj1.BoolValue == obj2.BoolValue;
        }
    }

    /// <summary>
    /// Simple test object for property-based testing of extension methods.
    /// </summary>
    public class SimpleTestObject
    {
        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public bool BoolValue { get; set; }
    }
}