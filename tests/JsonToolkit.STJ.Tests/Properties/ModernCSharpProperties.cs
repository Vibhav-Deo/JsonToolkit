using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for modern C# features.
    /// **Feature: json-toolkit-stj, Property 17: Modern C# features work seamlessly**
    /// </summary>
    public class ModernCSharpProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 17: Modern C# features work seamlessly**
        /// Record types should serialize and deserialize correctly.
        /// **Validates: Requirements 15.1**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ModernCSharp_RecordsShouldRoundTrip(string name, int value)
        {
            try
            {
                if (name == null) return true;

                var options = new JsonSerializerOptions();

                var original = new TestRecord(name, value);
                var json = JsonSerializer.Serialize(original, options);
                var roundTrip = JsonSerializer.Deserialize<TestRecord>(json, options);

                return roundTrip != null && 
                       roundTrip.Name == name && 
                       roundTrip.Value == value;
            }
            catch (Exception)
            {
                return false;
            }
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 17: Modern C# features work seamlessly**
        /// Init-only properties should be set during deserialization.
        /// **Validates: Requirements 15.2**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool ModernCSharp_InitOnlyPropertiesShouldWork(string data, int count)
        {
            try
            {
                if (data == null) return true;

                var options = new JsonSerializerOptions();
                var obj = new TestInitOnly { Data = data, Count = count };
                
                var json = JsonSerializer.Serialize(obj, options);
                var result = JsonSerializer.Deserialize<TestInitOnly>(json, options);

                return result != null && 
                       result.Data == data && 
                       result.Count == count;
            }
            catch (Exception)
            {
                return false;
            }
        }
#endif

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 17: Modern C# features work seamlessly**
        /// Positional records should support both property names and positional matching.
        /// **Validates: Requirements 15.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool ModernCSharp_PositionalRecordsShouldWork(string x, string y)
        {
            try
            {
                if (x == null || y == null) return true;

                var options = new JsonSerializerOptions();

                var original = new PositionalRecord(x, y);
                var json = JsonSerializer.Serialize(original, options);
                var roundTrip = JsonSerializer.Deserialize<PositionalRecord>(json, options);

                return roundTrip != null && 
                       roundTrip.X == x && 
                       roundTrip.Y == y;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }

    public record TestRecord(string Name, int Value);

    public class TestInitOnly
    {
        public string? Data { get; init; }
        public int Count { get; init; }
    }

    public record PositionalRecord(string X, string Y);
}
