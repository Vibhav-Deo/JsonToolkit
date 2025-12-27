using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using JsonToolkit.STJ;
using JsonToolkit.STJ.ValidationAttributes;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for enhanced JsonSerializer static methods.
    /// </summary>
    public class EnhancedSerializerTests
    {
        [Fact]
        public void SerializeEnhanced_SimpleObject_ShouldReturnValidJson()
        {
            // Arrange
            var testObj = new { Name = "Test", Value = 42 };

            // Act
            var json = JsonSerializerExtensions.SerializeEnhanced(testObj);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("Test", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void DeserializeEnhanced_ValidJson_ShouldReturnObject()
        {
            // Arrange
            var json = """{"Name":"Test","Value":42}""";

            // Act
            var result = JsonSerializerExtensions.DeserializeEnhanced<TestObject>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void SerializeEnhancedToUtf8Bytes_SimpleObject_ShouldReturnValidBytes()
        {
            // Arrange
            var testObj = new { Name = "Test", Value = 42 };

            // Act
            var bytes = JsonSerializerExtensions.SerializeEnhancedToUtf8Bytes(testObj);

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            
            var json = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Test", json);
        }

        [Fact]
        public void DeserializeEnhanced_Utf8Bytes_ShouldReturnObject()
        {
            // Arrange
            var json = """{"Name":"Test","Value":42}""";
            var bytes = Encoding.UTF8.GetBytes(json);

            // Act
            var result = JsonSerializerExtensions.DeserializeEnhanced<TestObject>(bytes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void DeserializeEnhanced_ReadOnlySpan_ShouldReturnObject()
        {
            // Arrange
            var json = """{"Name":"Test","Value":42}""";
            var bytes = Encoding.UTF8.GetBytes(json);
            var span = new ReadOnlySpan<byte>(bytes);

            // Act
            var result = JsonSerializerExtensions.DeserializeEnhanced<TestObject>(span);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task SerializeEnhancedAsync_SimpleObject_ShouldWriteToStream()
        {
            // Arrange
            var testObj = new { Name = "Test", Value = 42 };
            using var stream = new MemoryStream();

            // Act
            await JsonSerializerExtensions.SerializeEnhancedAsync(stream, testObj);

            // Assert
            Assert.True(stream.Length > 0);
            
            stream.Position = 0;
            var json = await new StreamReader(stream).ReadToEndAsync();
            Assert.Contains("Test", json);
        }

        [Fact]
        public async Task DeserializeEnhancedAsync_ValidStream_ShouldReturnObject()
        {
            // Arrange
            var json = """{"Name":"Test","Value":42}""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await JsonSerializerExtensions.DeserializeEnhancedAsync<TestObject>(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void SerializeEnhanced_WithCustomOptions_ShouldRespectOptions()
        {
            // Arrange
            var testObj = new { Name = "Test", Value = 42 };
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            var json = JsonSerializerExtensions.SerializeEnhanced(testObj, options);

            // Assert
            Assert.Contains("\n", json); // Should be indented
        }

        [Fact]
        public void DeserializeEnhanced_InvalidJson_ShouldThrowJsonToolkitException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act & Assert
            var exception = Assert.Throws<JsonToolkitException>(() =>
                JsonSerializerExtensions.DeserializeEnhanced<TestObject>(invalidJson));
            
            Assert.Contains("Failed to deserialize JSON", exception.Message);
            Assert.Equal("DeserializeEnhanced", exception.Operation);
        }

        [Fact]
        public void SerializeEnhanced_NullArgument_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                JsonSerializerExtensions.DeserializeEnhanced<TestObject>((string)null!));
        }

        [Fact]
        public void EnhancedSerializer_CompatibilityWithStandard_ShouldProduceEquivalentResults()
        {
            // Arrange
            var testObj = new TestObject { Name = "Test", Value = 42 };

            // Act
            var enhancedJson = JsonSerializerExtensions.SerializeEnhanced(testObj);
            var standardJson = JsonSerializer.Serialize(testObj);

            var enhancedResult = JsonSerializerExtensions.DeserializeEnhanced<TestObject>(enhancedJson);
            var standardResult = JsonSerializer.Deserialize<TestObject>(standardJson);

            // Cross-compatibility
            var crossResult1 = JsonSerializerExtensions.DeserializeEnhanced<TestObject>(standardJson);
            var crossResult2 = JsonSerializer.Deserialize<TestObject>(enhancedJson);

            // Assert
            Assert.Equal(testObj.Name, enhancedResult!.Name);
            Assert.Equal(testObj.Value, enhancedResult.Value);
            Assert.Equal(testObj.Name, standardResult!.Name);
            Assert.Equal(testObj.Value, standardResult.Value);
            Assert.Equal(testObj.Name, crossResult1!.Name);
            Assert.Equal(testObj.Value, crossResult1.Value);
            Assert.Equal(testObj.Name, crossResult2!.Name);
            Assert.Equal(testObj.Value, crossResult2.Value);
        }

        // New tests for automatic validation enforcement

        [Fact]
        public void Deserialize_WithValidationAttributes_ShouldValidateAutomatically()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions().WithValidation();

            // Act & Assert
            var exception = Assert.Throws<JsonValidationException>(() =>
                JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options));
            
            Assert.NotNull(exception.ValidationErrors);
            Assert.True(exception.ValidationErrors.Count > 0);
            Assert.Contains(exception.ValidationErrors, e => e.PropertyPath == "Name");
            Assert.Contains(exception.ValidationErrors, e => e.PropertyPath == "Age");
        }

        [Fact]
        public void Deserialize_WithValidationAttributes_ValidData_ShouldSucceed()
        {
            // Arrange
            var validJson = """{"Name":"ValidName","Age":25}""";
            var options = new JsonSerializerOptions().WithValidation();

            // Act
            var result = JsonSerializer.Deserialize<ValidatedTestObject>(validJson, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ValidName", result.Name);
            Assert.Equal(25, result.Age);
        }

        [Fact]
        public void Deserialize_EnableJsonToolkit_ShouldValidateByDefault()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions().EnableJsonToolkit();

            // Act & Assert
            var exception = Assert.Throws<JsonValidationException>(() =>
                JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options));
            
            Assert.NotNull(exception.ValidationErrors);
            Assert.True(exception.ValidationErrors.Count > 0);
        }

        [Fact]
        public void Deserialize_WithoutValidationAttributes_ShouldNotValidate()
        {
            // Arrange
            var json = """{"Name":"AnyName","Value":999}""";
            var options = new JsonSerializerOptions().WithValidation();

            // Act - Should not throw because TestObject has no validation attributes
            var result = JsonSerializer.Deserialize<TestObject>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AnyName", result.Name);
            Assert.Equal(999, result.Value);
        }

        [Fact]
        public void Deserialize_ValidationErrorMessages_ShouldBeDescriptive()
        {
            // Arrange
            var invalidJson = """{"Name":"","Age":150}"""; // Empty name, age too high
            var options = new JsonSerializerOptions().WithValidation();

            // Act & Assert
            var exception = Assert.Throws<JsonValidationException>(() =>
                JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options));
            
            Assert.NotNull(exception.ValidationErrors);
            Assert.True(exception.ValidationErrors.Count > 0);
            
            // Check that error messages are descriptive
            var nameError = exception.ValidationErrors.Where(e => e.PropertyPath == "Name").FirstOrDefault();
            var ageError = exception.ValidationErrors.Where(e => e.PropertyPath == "Age").FirstOrDefault();
            
            Assert.NotNull(nameError);
            Assert.NotNull(ageError);
            Assert.Contains("length", nameError.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("must be", ageError.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Deserialize_MultipleValidationErrors_ShouldCollectAll()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5,"Email":"invalid"}"""; // All fields invalid
            var options = new JsonSerializerOptions().WithValidation();

            // Act & Assert
            var exception = Assert.Throws<JsonValidationException>(() =>
                JsonSerializer.Deserialize<ComplexValidatedObject>(invalidJson, options));
            
            Assert.NotNull(exception.ValidationErrors);
            Assert.True(exception.ValidationErrors.Count >= 3); // Should have errors for all three properties
            
            var propertyPaths = new HashSet<string>(exception.ValidationErrors.Select(e => e.PropertyPath));
            Assert.Contains("Name", propertyPaths);
            Assert.Contains("Age", propertyPaths);
            Assert.Contains("Email", propertyPaths);
        }

        // New tests for validation opt-out scenarios

        [Fact]
        public void Deserialize_WithoutValidation_ShouldAllowInvalidData()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions().WithValidation().WithoutValidation();

            // Act - Should not throw because validation is explicitly disabled
            var result = JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("x", result.Name);
            Assert.Equal(-5, result.Age);
        }

        [Fact]
        public void Deserialize_WithoutValidation_PerformanceScenario_ShouldSucceed()
        {
            // Arrange
            var invalidJson = """{"Name":"","Age":999}"""; // Empty name, age out of range
            var options = new JsonSerializerOptions().WithoutValidation();

            // Act - Should succeed for performance-critical scenarios
            var result = JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.Name);
            Assert.Equal(999, result.Age);
        }

        [Fact]
        public void Deserialize_EnableThenDisableValidation_ShouldRespectLastSetting()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions()
                .WithValidation()      // Enable validation
                .WithoutValidation();  // Then disable it

            // Act - Should not throw because validation was disabled last
            var result = JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("x", result.Name);
            Assert.Equal(-5, result.Age);
        }

        [Fact]
        public void Deserialize_DisableThenEnableValidation_ShouldRespectLastSetting()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions()
                .WithoutValidation()   // Disable validation
                .WithValidation();     // Then enable it

            // Act & Assert - Should throw because validation was enabled last
            var exception = Assert.Throws<JsonValidationException>(() =>
                JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options));
            
            Assert.NotNull(exception.ValidationErrors);
            Assert.True(exception.ValidationErrors.Count > 0);
        }

        [Fact]
        public void Deserialize_WithoutValidation_MultipleCallsShouldBeIdempotent()
        {
            // Arrange
            var invalidJson = """{"Name":"x","Age":-5}"""; // Name too short, Age negative
            var options = new JsonSerializerOptions()
                .WithValidation()
                .WithoutValidation()
                .WithoutValidation()   // Multiple calls should be safe
                .WithoutValidation();

            // Act - Should not throw
            var result = JsonSerializer.Deserialize<ValidatedTestObject>(invalidJson, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("x", result.Name);
            Assert.Equal(-5, result.Age);
        }

        public class TestObject
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        public class ValidatedTestObject
        {
            [JsonLength(2, 100)]
            public string Name { get; set; } = string.Empty;

            [JsonRange(0, 120)]
            public int Age { get; set; }
        }

        public class ComplexValidatedObject
        {
            [JsonLength(2, 100)]
            public string Name { get; set; } = string.Empty;

            [JsonRange(0, 120)]
            public int Age { get; set; }

            [JsonPattern(@"^[^@]+@[^@]+\.[^@]+$")]
            public string Email { get; set; } = string.Empty;
        }
    }
}