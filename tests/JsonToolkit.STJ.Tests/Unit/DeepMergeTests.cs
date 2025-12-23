using System;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for deep merge functionality.
    /// </summary>
    public class DeepMergeTests
    {
        [Fact]
        public void DeepMerge_JsonElements_ShouldMergeCorrectly()
        {
            // Arrange
            var targetJson = """{"name": "target", "value": 1, "nested": {"prop": "target_nested"}}""";
            var sourceJson = """{"name": "source", "other": 2, "nested": {"prop": "source_nested", "new": "added"}}""";
            
            var targetElement = JsonDocument.Parse(targetJson).RootElement;
            var sourceElement = JsonDocument.Parse(sourceJson).RootElement;
            
            // Act
            var merged = JsonMerge.DeepMerge(targetElement, sourceElement);
            
            // Assert
            Assert.Equal("source", merged.GetProperty("name").GetString());
            Assert.Equal(1, merged.GetProperty("value").GetInt32());
            Assert.Equal(2, merged.GetProperty("other").GetInt32());
            
            var nestedMerged = merged.GetProperty("nested");
            Assert.Equal("source_nested", nestedMerged.GetProperty("prop").GetString());
            Assert.Equal("added", nestedMerged.GetProperty("new").GetString());
        }

        [Fact]
        public void DeepMerge_Objects_ShouldMergeCorrectly()
        {
            // Arrange
            var target = new TestObject
            {
                Name = "target",
                Value = 1,
                Nested = new NestedObject { NestedValue = "target_nested" }
            };
            
            var source = new TestObject
            {
                Name = "source",
                Value = 2,
                Nested = new NestedObject { NestedValue = "source_nested" }
            };
            
            // Act
            var merged = JsonMerge.DeepMerge(target, source);
            
            // Assert
            Assert.Equal("source", merged.Name);
            Assert.Equal(2, merged.Value);
            Assert.Equal("source_nested", merged.Nested?.NestedValue);
        }

        [Fact]
        public void DeepMerge_NullHandling_ShouldWorkCorrectly()
        {
            // Arrange
            var target = new TestObject { Name = "target", Value = 1 };
            TestObject? source = null;
            
            // Act
            var merged = JsonMerge.DeepMerge(target, source);
            
            // Assert
            Assert.Equal("target", merged?.Name);
            Assert.Equal(1, merged?.Value);
        }

        [Fact]
        public void DeepMerge_ArrayReplacement_ShouldReplaceNotMerge()
        {
            // Arrange
            var targetJson = """{"items": [1, 2, 3]}""";
            var sourceJson = """{"items": [4, 5]}""";
            
            var targetElement = JsonDocument.Parse(targetJson).RootElement;
            var sourceElement = JsonDocument.Parse(sourceJson).RootElement;
            
            // Act
            var merged = JsonMerge.DeepMerge(targetElement, sourceElement);
            
            // Assert
            var items = merged.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
            Assert.Equal(4, items[0].GetInt32());
            Assert.Equal(5, items[1].GetInt32());
        }

        [Fact]
        public void DeepMerge_TypeConflicts_ShouldReplaceWithSource()
        {
            // Arrange
            var targetJson = """{"prop": "string_value"}""";
            var sourceJson = """{"prop": 42}""";
            
            var targetElement = JsonDocument.Parse(targetJson).RootElement;
            var sourceElement = JsonDocument.Parse(sourceJson).RootElement;
            
            // Act
            var merged = JsonMerge.DeepMerge(targetElement, sourceElement);
            
            // Assert
            var prop = merged.GetProperty("prop");
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);
            Assert.Equal(42, prop.GetInt32());
        }

        [Fact]
        public void DeepMerge_NullValues_ShouldOverwrite()
        {
            // Arrange
            var targetJson = """{"name": "target", "value": 42}""";
            var sourceJson = """{"name": null, "other": "added"}""";
            
            var targetElement = JsonDocument.Parse(targetJson).RootElement;
            var sourceElement = JsonDocument.Parse(sourceJson).RootElement;
            
            // Act
            var merged = JsonMerge.DeepMerge(targetElement, sourceElement);
            
            // Assert
            Assert.Equal(JsonValueKind.Null, merged.GetProperty("name").ValueKind);
            Assert.Equal(42, merged.GetProperty("value").GetInt32());
            Assert.Equal("added", merged.GetProperty("other").GetString());
        }
    }

    public class TestObject
    {
        public string? Name { get; set; }
        public int? Value { get; set; }
        public int[]? Items { get; set; }
        public NestedObject? Nested { get; set; }
    }

    public class NestedObject
    {
        public string? NestedValue { get; set; }
    }
}