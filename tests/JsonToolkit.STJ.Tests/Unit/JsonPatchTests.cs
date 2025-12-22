using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for JSON Patch functionality.
    /// </summary>
    public class JsonPatchTests
    {
        [Fact]
        public void JsonPatch_Add_ShouldAddProperty()
        {
            // Arrange
            var originalJson = """{"name": "test"}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Add("/value", 42);
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.Equal(42, patchedElement.GetProperty("value").GetInt32());
        }

        [Fact]
        public void JsonPatch_Remove_ShouldRemoveProperty()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Remove("/value");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.False(patchedElement.TryGetProperty("value", out _));
        }

        [Fact]
        public void JsonPatch_Replace_ShouldReplaceProperty()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Replace("/value", 100);
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.Equal(100, patchedElement.GetProperty("value").GetInt32());
        }

        [Fact]
        public void JsonPatch_Test_ShouldPassWhenValueMatches()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Test("/value", 42)
                .Add("/newProp", "added");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.Equal(42, patchedElement.GetProperty("value").GetInt32());
            Assert.Equal("added", patchedElement.GetProperty("newProp").GetString());
        }

        [Fact]
        public void JsonPatch_Test_ShouldFailWhenValueDoesNotMatch()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Test("/value", 999)
                .Add("/newProp", "added");
            
            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() => patch.ApplyTo(originalElement));
            Assert.Contains("Test operation failed", exception.Message);
        }

        [Fact]
        public void JsonPatch_Move_ShouldMoveProperty()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Move("/value", "/newValue");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.False(patchedElement.TryGetProperty("value", out _));
            Assert.Equal(42, patchedElement.GetProperty("newValue").GetInt32());
        }

        [Fact]
        public void JsonPatch_Copy_ShouldCopyProperty()
        {
            // Arrange
            var originalJson = """{"name": "test", "value": 42}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Copy("/value", "/valueCopy");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal("test", patchedElement.GetProperty("name").GetString());
            Assert.Equal(42, patchedElement.GetProperty("value").GetInt32());
            Assert.Equal(42, patchedElement.GetProperty("valueCopy").GetInt32());
        }

        [Fact]
        public void JsonPatch_CreateIntermediatePaths_ShouldCreateNestedStructure()
        {
            // Arrange
            var originalElement = JsonDocument.Parse("{}").RootElement;
            
            var patch = new JsonPatchDocument()
                .Add("/level1/level2/value", "deep");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            var level1 = patchedElement.GetProperty("level1");
            var level2 = level1.GetProperty("level2");
            Assert.Equal("deep", level2.GetProperty("value").GetString());
        }

        [Fact]
        public void JsonPatch_ArrayOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var originalJson = """{"items": [1, 2, 3]}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Replace("/items/0", 100);  // Replace first element only
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            var items = patchedElement.GetProperty("items");
            Assert.Equal(3, items.GetArrayLength());
            Assert.Equal(100, items[0].GetInt32());
            Assert.Equal(2, items[1].GetInt32());
            Assert.Equal(3, items[2].GetInt32());
        }

        [Fact]
        public void JsonPatch_ObjectPatching_ShouldWorkCorrectly()
        {
            // Arrange
            var originalObject = new TestPatchObject
            {
                Name = "original",
                Value = 42,
                Items = new[] { 1, 2, 3 }
            };
            
            var patch = new JsonPatchDocument()
                .Replace("/Name", "patched")
                .Add("/NewProperty", "added");
            
            // Act
            var patchedObject = patch.ApplyTo(originalObject);
            
            // Assert
            Assert.Equal("patched", patchedObject.Name);
            Assert.Equal(42, patchedObject.Value);
            Assert.Equal(new[] { 1, 2, 3 }, patchedObject.Items);
        }

        [Fact]
        public void JsonPatch_SequentialOperations_ShouldExecuteInOrder()
        {
            // Arrange
            var originalJson = """{"counter": 0}""";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;
            
            var patch = new JsonPatchDocument()
                .Replace("/counter", 1)
                .Add("/temp", 10)
                .Copy("/temp", "/backup")
                .Replace("/counter", 2)
                .Remove("/temp");
            
            // Act
            var patchedElement = patch.ApplyTo(originalElement);
            
            // Assert
            Assert.Equal(2, patchedElement.GetProperty("counter").GetInt32());
            Assert.Equal(10, patchedElement.GetProperty("backup").GetInt32());
            Assert.False(patchedElement.TryGetProperty("temp", out _));
        }
    }

    public class TestPatchObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public int[]? Items { get; set; }
    }
}