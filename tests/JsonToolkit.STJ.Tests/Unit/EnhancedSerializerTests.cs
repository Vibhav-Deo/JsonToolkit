using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using JsonToolkit.STJ;

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

        public class TestObject
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}