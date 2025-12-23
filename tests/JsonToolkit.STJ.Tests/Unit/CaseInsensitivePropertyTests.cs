using System;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for case-insensitive property matching functionality.
    /// </summary>
    public class CaseInsensitivePropertyTests
    {
        /// <summary>
        /// Test that case-insensitive property matching works by default.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_ShouldWorkByDefault()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var testObject = new CaseInsensitiveTestObject { Name = "John", Value = 42, IsActive = true };
            var json = JsonSerializer.Serialize(testObject, options);

            // Test various casing variations
            var variations = new[]
            {
                json.Replace("\"Name\"", "\"name\""),
                json.Replace("\"Name\"", "\"NAME\""),
                json.Replace("\"Value\"", "\"value\""),
                json.Replace("\"IsActive\"", "\"isactive\"")
            };

            // Act & Assert
            foreach (var variation in variations)
            {
                var deserialized = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(variation, options);
                
                Assert.NotNull(deserialized);
                Assert.Equal(testObject.Name, deserialized.Name);
                Assert.Equal(testObject.Value, deserialized.Value);
                Assert.Equal(testObject.IsActive, deserialized.IsActive);
            }
        }

        /// <summary>
        /// Test that strict mode requires exact case matching.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_StrictModeShouldRequireExactCase()
        {
            // Arrange
            var strictOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false // Strict mode
            };

            var testObject = new CaseInsensitiveTestObject { Name = "John", Value = 42, IsActive = true };
            var json = JsonSerializer.Serialize(testObject, strictOptions);

            // Act - exact case should work
            var exactMatch = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(json, strictOptions);
            
            // Assert
            Assert.NotNull(exactMatch);
            Assert.Equal(testObject.Name, exactMatch.Name);
            Assert.Equal(testObject.Value, exactMatch.Value);

            // Act - different case should ignore the property
            var differentCaseJson = json.Replace("\"Name\"", "\"name\"");
            var differentCaseResult = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(differentCaseJson, strictOptions);

            // Assert - Name should be null/default because "name" doesn't match "Name"
            Assert.NotNull(differentCaseResult);
            Assert.Null(differentCaseResult.Name); // Property was ignored due to case mismatch
            Assert.Equal(testObject.Value, differentCaseResult.Value); // Other properties should still work
        }

        /// <summary>
        /// Test that enhanced case-insensitive options work correctly.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_EnhancedOptionsShouldWork()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            options.WithEnhancedCaseInsensitiveProperties();

            var testObject = new CaseInsensitiveTestObject { Name = "John", Value = 42, IsActive = true };

            // Act
            var json = JsonSerializer.Serialize(testObject, options);
            var deserialized = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testObject.Name, deserialized.Name);
            Assert.Equal(testObject.Value, deserialized.Value);
            Assert.Equal(testObject.IsActive, deserialized.IsActive);
        }

        /// <summary>
        /// Test that strict case-sensitive options work correctly.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_StrictCaseSensitiveOptionsShouldWork()
        {
            // Arrange
            var options = new JsonSerializerOptions();
            options.WithStrictCaseSensitiveProperties();

            var testObject = new CaseInsensitiveTestObject { Name = "John", Value = 42, IsActive = true };

            // Act
            var json = JsonSerializer.Serialize(testObject, options);
            var deserialized = JsonSerializer.Deserialize<CaseInsensitiveTestObject>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testObject.Name, deserialized.Name);
            Assert.Equal(testObject.Value, deserialized.Value);
            Assert.Equal(testObject.IsActive, deserialized.IsActive);

            // Verify that PropertyNameCaseInsensitive is false
            Assert.False(options.PropertyNameCaseInsensitive);
        }

        /// <summary>
        /// Test that special characters in property names are handled consistently.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_ShouldHandleSpecialCharactersConsistently()
        {
            // Arrange
            var caseInsensitiveOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var strictOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            };

            var json = "{\"property_name\": 42, \"property-name\": \"test\"}";

            // Act
            var caseInsensitiveResult = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json, caseInsensitiveOptions);
            var strictResult = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json, strictOptions);

            // Assert - both should handle the same property names consistently
            Assert.NotNull(caseInsensitiveResult);
            Assert.NotNull(strictResult);
            Assert.Equal(2, caseInsensitiveResult.Count);
            Assert.Equal(2, strictResult.Count);
            Assert.True(caseInsensitiveResult.ContainsKey("property_name"));
            Assert.True(caseInsensitiveResult.ContainsKey("property-name"));
            Assert.True(strictResult.ContainsKey("property_name"));
            Assert.True(strictResult.ContainsKey("property-name"));
        }

        /// <summary>
        /// Test that data integrity is preserved during case-insensitive matching.
        /// </summary>
        [Fact]
        public void CaseInsensitivePropertyMatching_ShouldPreserveDataIntegrity()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var testObject = new CaseInsensitiveComplexTestObject
            {
                StringProperty = "Hello World",
                IntProperty = 12345,
                BoolProperty = true,
                DoubleProperty = 3.14159
            };

            // Act
            var json = JsonSerializer.Serialize(testObject, options);
            var roundTrip = JsonSerializer.Deserialize<CaseInsensitiveComplexTestObject>(json, options);

            // Assert
            Assert.NotNull(roundTrip);
            Assert.Equal(testObject.StringProperty, roundTrip.StringProperty);
            Assert.Equal(testObject.IntProperty, roundTrip.IntProperty);
            Assert.Equal(testObject.BoolProperty, roundTrip.BoolProperty);
            Assert.Equal(testObject.DoubleProperty, roundTrip.DoubleProperty, 10); // Allow for small floating point differences
        }

        /// <summary>
        /// Test that JsonOptionsBuilder supports case-insensitive configuration.
        /// </summary>
        [Fact]
        public void JsonOptionsBuilder_ShouldSupportCaseInsensitiveConfiguration()
        {
            // Arrange & Act
            var options = new JsonOptionsBuilder()
                .WithCaseInsensitiveProperties()
                .Build();

            // Assert
            Assert.True(options.PropertyNameCaseInsensitive);
        }

        /// <summary>
        /// Test that JsonOptionsBuilder supports enhanced case-insensitive configuration.
        /// </summary>
        [Fact]
        public void JsonOptionsBuilder_ShouldSupportEnhancedCaseInsensitiveConfiguration()
        {
            // Arrange & Act
            var options = new JsonOptionsBuilder()
                .WithEnhancedCaseInsensitiveProperties(opts =>
                {
                    opts.StrictMode = false;
                    opts.ThrowOnAmbiguity = false;
                })
                .Build();

            // Assert
            Assert.True(options.PropertyNameCaseInsensitive);
        }

        /// <summary>
        /// Test that JsonOptionsBuilder supports strict case-sensitive configuration.
        /// </summary>
        [Fact]
        public void JsonOptionsBuilder_ShouldSupportStrictCaseSensitiveConfiguration()
        {
            // Arrange & Act
            var options = new JsonOptionsBuilder()
                .WithStrictCaseSensitiveProperties()
                .Build();

            // Assert
            Assert.False(options.PropertyNameCaseInsensitive);
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
}