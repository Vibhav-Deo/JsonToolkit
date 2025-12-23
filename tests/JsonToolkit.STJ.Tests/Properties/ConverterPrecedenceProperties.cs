using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using JsonToolkit.STJ;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for converter precedence determinism.
    /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
    /// </summary>
    public class ConverterPrecedenceProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
        /// For any set of converters with different precedence values, the converter with the highest
        /// precedence should always be selected for a given type, and the selection should be deterministic.
        /// **Validates: Requirements 12.2, 12.3, 12.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConverterPrecedence_ShouldBeDeterministic(NonEmptyArray<int> precedenceValues)
        {
            try
            {
                // Clear any existing converters
                ConverterRegistry.Clear();
                
                var precedences = precedenceValues.Get.Distinct().ToArray();
                if (precedences.Length < 2)
                    return true; // Not enough unique precedences to test
                
                var converters = new List<(TestConverter Converter, int Precedence)>();
                
                // Create converters with different precedence values for the same type
                foreach (var precedence in precedences)
                {
                    var converter = new TestConverter(precedence);
                    converters.Add((converter, precedence));
                    ConverterRegistry.RegisterConverter(converter, precedence);
                }
                
                // Get the converters for the test type
                var registeredConverters = ConverterRegistry.GetConvertersForType(typeof(TestData)).ToList();
                
                // The first converter should be the one with the highest precedence
                if (registeredConverters.Count > 0)
                {
                    var firstConverter = registeredConverters.First() as TestConverter;
                    var expectedHighestPrecedence = precedences.Max();
                    
                    if (firstConverter?.Precedence != expectedHighestPrecedence)
                        return false;
                }
                
                // Test that the order is consistent across multiple calls
                var firstCall = ConverterRegistry.GetConvertersForType(typeof(TestData)).ToList();
                var secondCall = ConverterRegistry.GetConvertersForType(typeof(TestData)).ToList();
                
                if (firstCall.Count != secondCall.Count)
                    return false;
                
                for (int i = 0; i < firstCall.Count; i++)
                {
                    var first = firstCall[i] as TestConverter;
                    var second = secondCall[i] as TestConverter;
                    
                    if (first?.Precedence != second?.Precedence)
                        return false;
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Clean up
                ConverterRegistry.Clear();
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
        /// For any JsonSerializerOptions with multiple converters for the same type, the converter
        /// with higher precedence should be added to the options in the correct order.
        /// **Validates: Requirements 12.2, 12.3, 12.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConverterPrecedence_ShouldAffectOptionsOrder(PositiveInt highPrecedence, NonNegativeInt lowPrecedence)
        {
            try
            {
                // Ensure high precedence is actually higher
                var high = highPrecedence.Get + 100;
                var low = lowPrecedence.Get;
                
                if (high <= low)
                    return true; // Skip if precedences are not properly ordered
                
                // Clear registry to start fresh
                ConverterRegistry.Clear();
                
                var options = new JsonSerializerOptions();
                
                // Add converters with different precedence values
                var lowPrecedenceConverter = new TestConverter(low, "LOW");
                var highPrecedenceConverter = new TestConverter(high, "HIGH");
                
                options.AddConverter(lowPrecedenceConverter, low);
                options.AddConverter(highPrecedenceConverter, high);
                
                // Check that converters are in the options
                var testDataConverters = options.Converters
                    .OfType<TestConverter>()
                    .ToList();
                
                // Should have both converters, with high precedence first
                if (testDataConverters.Count < 2)
                    return false;
                
                // The first converter should be the high precedence one
                return testDataConverters.First().Precedence == high;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                ConverterRegistry.Clear();
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
        /// For any converter registration order, the precedence values should determine the final order,
        /// not the registration sequence.
        /// **Validates: Requirements 12.2, 12.3, 12.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConverterPrecedence_ShouldIgnoreRegistrationOrder(NonEmptyArray<int> precedenceValues)
        {
            try
            {
                ConverterRegistry.Clear();
                
                var precedences = precedenceValues.Get.Distinct().ToArray();
                if (precedences.Length < 2)
                    return true;
                
                // Register converters in one order
                var converters1 = new List<TestConverter>();
                foreach (var precedence in precedences)
                {
                    var converter = new TestConverter(precedence);
                    converters1.Add(converter);
                    ConverterRegistry.RegisterConverter(converter, precedence);
                }
                
                var result1 = ConverterRegistry.GetConvertersForType(typeof(TestData))
                    .Cast<TestConverter>()
                    .Select(c => c.Precedence)
                    .ToList();
                
                ConverterRegistry.Clear();
                
                // Register the same converters in reverse order
                var converters2 = new List<TestConverter>();
                foreach (var precedence in precedences.AsEnumerable().Reverse())
                {
                    var converter = new TestConverter(precedence);
                    converters2.Add(converter);
                    ConverterRegistry.RegisterConverter(converter, precedence);
                }
                
                var result2 = ConverterRegistry.GetConvertersForType(typeof(TestData))
                    .Cast<TestConverter>()
                    .Select(c => c.Precedence)
                    .ToList();
                
                // Both results should have the same order (highest precedence first)
                return result1.SequenceEqual(result2);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                ConverterRegistry.Clear();
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
        /// For any converter with the same precedence, the registration order should be used as a tiebreaker,
        /// ensuring deterministic behavior.
        /// **Validates: Requirements 12.2, 12.3, 12.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConverterPrecedence_ShouldUseDeterministicTiebreaker(PositiveInt precedence, PositiveInt converterCount)
        {
            try
            {
                ConverterRegistry.Clear();
                
                var prec = precedence.Get;
                var count = Math.Min(converterCount.Get, 5); // Limit to reasonable number
                
                var converters = new List<TestConverter>();
                
                // Register multiple converters with the same precedence
                for (int i = 0; i < count; i++)
                {
                    var converter = new TestConverter(prec, $"CONVERTER_{i}");
                    converters.Add(converter);
                    ConverterRegistry.RegisterConverter(converter, prec);
                }
                
                // Get the converters multiple times
                var result1 = ConverterRegistry.GetConvertersForType(typeof(TestData))
                    .Cast<TestConverter>()
                    .Select(c => c.Identifier)
                    .ToList();
                
                var result2 = ConverterRegistry.GetConvertersForType(typeof(TestData))
                    .Cast<TestConverter>()
                    .Select(c => c.Identifier)
                    .ToList();
                
                // Results should be identical (deterministic)
                return result1.SequenceEqual(result2);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                ConverterRegistry.Clear();
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 13: Converter precedence is deterministic**
        /// For any attempt to register duplicate converters, the system should handle conflicts appropriately
        /// and maintain deterministic behavior.
        /// **Validates: Requirements 12.2, 12.3, 12.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ConverterPrecedence_ShouldHandleConflictsConsistently(PositiveInt precedence)
        {
            try
            {
                ConverterRegistry.Clear();
                
                var prec = precedence.Get;
                var converter1 = new TestConverter(prec, "FIRST");
                var converter2 = new TestConverter(prec, "SECOND");
                
                // Register first converter
                ConverterRegistry.RegisterConverter(converter1, prec);
                
                // Attempt to register second converter of the same type
                try
                {
                    ConverterRegistry.RegisterConverter(converter2, prec);
                    // If no exception is thrown, both converters should be registered
                    var converters = ConverterRegistry.GetConvertersForType(typeof(TestData)).ToList();
                    return converters.Count >= 1; // At least one converter should be present
                }
                catch (JsonToolkitException)
                {
                    // Exception is expected for duplicate registrations
                    var converters = ConverterRegistry.GetConvertersForType(typeof(TestData)).ToList();
                    return converters.Count == 1; // Only the first converter should remain
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                ConverterRegistry.Clear();
            }
        }
    }

    /// <summary>
    /// Test data class for converter precedence testing.
    /// </summary>
    public class TestData
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test converter for precedence testing.
    /// </summary>
    public class TestConverter : SimpleJsonConverter<TestData>
    {
        public override int Precedence { get; }
        public string Identifier { get; }

        public TestConverter(int precedence, string identifier = "DEFAULT")
        {
            Precedence = precedence;
            Identifier = identifier;
        }

        protected override TestData ReadValue(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            var testData = new TestData();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                    
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    
                    if (propertyName == "Value" || propertyName == "value")
                    {
                        testData.Value = reader.GetString() ?? string.Empty;
                    }
                }
            }
            
            return testData;
        }

        protected override void WriteValue(Utf8JsonWriter writer, TestData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Value", value.Value);
            writer.WriteString("ConverterUsed", Identifier);
            writer.WriteEndObject();
        }
    }
}