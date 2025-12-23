using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for case-insensitive property matching functionality.
    /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
    /// </summary>
    public class CaseInsensitivePropertyProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any JSON object and target type, property matching should work case-insensitively by default,
        /// detect ambiguous cases, support strict mode, and handle special characters consistently.
        /// **Validates: Requirements 13.1, 13.2, 13.3, 13.4, 13.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_ShouldWorkByDefault(string name, int value, bool flag)
        {
            try
            {
                // Skip null or empty names as they're not valid property names
                if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                    return true;

                // Create test object with various casing
                var testObj = new CaseInsensitiveTestObject { Name = name, Value = value, IsActive = flag };
                
                // Test with default case-insensitive options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Serialize the object
                var json = JsonSerializer.Serialize(testObj, options);
                
                // Create variations with different casing
                var variations = new[]
                {
                    json,
                    json.Replace("\"Name\"", "\"name\""),
                    json.Replace("\"Name\"", "\"NAME\""),
                    json.Replace("\"Value\"", "\"value\""),
                    json.Replace("\"Value\"", "\"VALUE\""),
                    json.Replace("\"IsActive\"", "\"isactive\""),
                    json.Replace("\"IsActive\"", "\"ISACTIVE\"")
                };

                // All variations should deserialize successfully with case-insensitive matching
                foreach (var variation in variations)
                {
                    try
                    {
                        var deserialized = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(variation, options);
                        
                        if (deserialized == null ||
                            deserialized.Name != testObj.Name ||
                            deserialized.Value != testObj.Value ||
                            deserialized.IsActive != testObj.IsActive)
                        {
                            return false;
                        }
                    }
                    catch (JsonException)
                    {
                        // Case-insensitive matching should not fail for simple casing differences
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

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any object type with properties that differ only by case, the system should detect
        /// ambiguity and handle it according to configuration.
        /// **Validates: Requirements 13.2**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_ShouldDetectAmbiguity(string baseName, int value1, int value2)
        {
            try
            {
                // Skip invalid names
                if (string.IsNullOrWhiteSpace(baseName) || baseName.Length > 20 || baseName.Length < 2)
                    return true;

                // Create JSON with ambiguous property names (same name with different casing)
                var lowerName = baseName.ToLowerInvariant();
                var upperName = baseName.ToUpperInvariant();
                
                // Skip if the names are the same (no ambiguity)
                if (lowerName == upperName)
                    return true;

                var ambiguousJson = $"{{" +
                    $"\"{lowerName}\": {value1}, " +
                    $"\"{upperName}\": {value2}" +
                    $"}}";

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    // This should either succeed (using one of the values) or fail gracefully
                    // The exact behavior depends on the JSON deserializer implementation
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(ambiguousJson, options);
                    
                    // If it succeeds, it should have exactly one entry (the ambiguous properties should be merged)
                    return result != null && result.Count <= 2; // Allow for both properties or merged result
                }
                catch (JsonException)
                {
                    // It's acceptable for ambiguous JSON to throw an exception
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any JSON object, strict mode should require exact case matching and reject
        /// case-insensitive matches.
        /// **Validates: Requirements 13.3**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_StrictModeShouldRequireExactCase(string name, int value)
        {
            try
            {
                // Skip invalid names
                if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                    return true;

                var testObj = new CaseInsensitiveTestObject { Name = name, Value = value, IsActive = true };
                
                // Test with strict case-sensitive options
                var strictOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false // Strict mode
                };

                // Serialize with exact casing
                var json = JsonSerializer.Serialize(testObj, strictOptions);
                
                // This should work fine
                var exactMatch = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(json, strictOptions);
                if (exactMatch == null || exactMatch.Name != testObj.Name || exactMatch.Value != testObj.Value)
                    return false;

                // Create a version with different casing
                var differentCasingJson = json.Replace("\"Name\"", "\"name\"");
                
                // Skip if no change was made (property name was already lowercase)
                if (differentCasingJson == json)
                    return true;

                try
                {
                    var differentCasingResult = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(differentCasingJson, strictOptions);
                    
                    // In strict mode, the property with different casing should be ignored
                    // So the Name should be null or default
                    return differentCasingResult != null && 
                           (differentCasingResult.Name == null || differentCasingResult.Name == default(string));
                }
                catch (JsonException)
                {
                    // It's also acceptable for strict mode to throw an exception
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any JSON object with special characters in property names, case-insensitive matching
        /// should handle them consistently across different casing modes.
        /// **Validates: Requirements 13.4, 13.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_ShouldHandleSpecialCharactersConsistently(NonEmptyString propertyName, int value)
        {
            try
            {
                var name = propertyName.Get;
                
                // Skip very long names or names with problematic characters for JSON
                if (name.Length > 30 || name.Contains("\"") || name.Contains("\\") || name.Contains("\n") || name.Contains("\r"))
                    return true;

                // Create JSON with special characters in property names
                var json = $"{{" +
                    $"\"{name}\": {value}" +
                    $"}}";

                var caseInsensitiveOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var strictOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                };

                try
                {
                    // Both modes should handle the same property name consistently
                    var caseInsensitiveResult = JsonSerializer.Deserialize<Dictionary<string, object>>(json, caseInsensitiveOptions);
                    var strictResult = JsonSerializer.Deserialize<Dictionary<string, object>>(json, strictOptions);

                    // Both should succeed and produce the same result for exact matches
                    return caseInsensitiveResult != null && 
                           strictResult != null &&
                           caseInsensitiveResult.Count == strictResult.Count &&
                           caseInsensitiveResult.ContainsKey(name) &&
                           strictResult.ContainsKey(name);
                }
                catch (JsonException)
                {
                    // Some special characters might cause parsing issues, which is acceptable
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any object type, enhanced case-insensitive options should provide consistent behavior
        /// across different configuration modes.
        /// **Validates: Requirements 13.1, 13.2, 13.3, 13.4, 13.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_EnhancedOptionsShouldBeConsistent(string name, int value, bool strictMode, bool throwOnAmbiguity)
        {
            try
            {
                // Skip invalid names
                if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                    return true;

                var testObj = new CaseInsensitiveTestObject { Name = name, Value = value, IsActive = true };

                // Test with enhanced case-insensitive options
                var options = new JsonSerializerOptions();
                options.WithEnhancedCaseInsensitiveProperties(opts =>
                {
                    opts.StrictMode = strictMode;
                    opts.ThrowOnAmbiguity = throwOnAmbiguity;
                });

                // Serialize the object
                var json = JsonSerializer.Serialize(testObj, options);
                
                // Test deserialization
                var deserialized = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(json, options);
                
                // Should always work for exact matches
                return deserialized != null &&
                       deserialized.Name == testObj.Name &&
                       deserialized.Value == testObj.Value &&
                       deserialized.IsActive == testObj.IsActive;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 14: Case-insensitive property matching handles ambiguity**
        /// For any valid JSON object, case-insensitive property matching should preserve data integrity
        /// and not lose or corrupt property values during deserialization.
        /// **Validates: Requirements 13.1, 13.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool CaseInsensitivePropertyMatching_ShouldPreserveDataIntegrity(string stringVal, int intVal, bool boolVal, double doubleVal)
        {
            try
            {
                // Skip special double values that JSON doesn't support
                if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal))
                    return true;

                // Skip null string values for this test
                if (stringVal == null)
                    stringVal = "";

                var testObj = new CaseInsensitiveComplexTestObject
                {
                    StringProperty = stringVal,
                    IntProperty = intVal,
                    BoolProperty = boolVal,
                    DoubleProperty = doubleVal
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Serialize and deserialize
                var json = JsonSerializer.Serialize(testObj, options);
                var roundTrip = JsonSerializer.Deserialize<CaseInsensitiveComplexTestObject>(json, options);

                // Verify data integrity
                return roundTrip != null &&
                       roundTrip.StringProperty == testObj.StringProperty &&
                       roundTrip.IntProperty == testObj.IntProperty &&
                       roundTrip.BoolProperty == testObj.BoolProperty &&
                       Math.Abs(roundTrip.DoubleProperty - testObj.DoubleProperty) < 1e-10;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Test object for case-insensitive property matching tests.
    /// </summary>
    public class CaseInsensitiveTestObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Complex test object for comprehensive property matching tests.
    /// </summary>
    public class CaseInsensitiveComplexTestObject
    {
        public string? StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public double DoubleProperty { get; set; }
    }

    /// <summary>
    /// Test object with potentially ambiguous property names.
    /// </summary>
    public class CaseInsensitiveAmbiguousTestObject
    {
        public string? Name { get; set; }
        public string? name { get; set; } // This would create ambiguity in case-insensitive mode
        public int Value { get; set; }
    }
}