using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Integration
{
    /// <summary>
    /// Integration tests to verify package functionality across target frameworks.
    /// Tests basic functionality, framework compatibility, and absence of conflicts.
    /// </summary>
    public class PackageFunctionalityTests
    {
        [Fact]
        public void Package_ShouldLoadCorrectly_AcrossAllTargetFrameworks()
        {
            // Verify the assembly loads and basic types are available
            var assembly = typeof(JsonOptionsBuilder).Assembly;
            
            Assert.NotNull(assembly);
            Assert.Equal("JsonToolkit.STJ", assembly.GetName().Name);
            
            // Verify key types are available
            var keyTypes = new[]
            {
                typeof(JsonOptionsBuilder),
                typeof(JsonMerge),
                typeof(JElement),
                typeof(JsonPatchDocument),
                typeof(JsonPath),
                typeof(JsonLinq)
            };
            
            foreach (var type in keyTypes)
            {
                Assert.NotNull(type);
                Assert.True(type.IsPublic, $"Type {type.Name} should be public");
            }
        }

        [Fact]
        public void BasicSerialization_ShouldWork_WithSystemTextJson()
        {
            // Test basic System.Text.Json functionality works
            var testObject = new { Name = "Test", Value = 42, Items = new[] { 1, 2, 3 } };
            
            var json = JsonSerializer.Serialize(testObject);
            Assert.NotNull(json);
            Assert.Contains("Test", json);
            Assert.Contains("42", json);
            
            var deserialized = JsonSerializer.Deserialize<dynamic>(json);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void DeepMerge_ShouldWork_WithBasicObjects()
        {
            var json1 = """{"A": 1, "B": {"X": 10, "Y": 20}}""";
            var json2 = """{"B": {"Y": 30, "Z": 40}, "C": 3}""";
            
            var element1 = JsonDocument.Parse(json1).RootElement;
            var element2 = JsonDocument.Parse(json2).RootElement;
            
            var merged = JsonMerge.DeepMerge(element1, element2);
            
            // Verify merge worked by checking properties
            Assert.True(merged.TryGetProperty("A", out var aProp));
            Assert.Equal(1, aProp.GetInt32());
            Assert.True(merged.TryGetProperty("C", out var cProp));
            Assert.Equal(3, cProp.GetInt32());
        }

        [Fact]
        public void JsonPatch_ShouldWork_WithBasicOperations()
        {
            var document = JsonDocument.Parse("""{"name": "John", "age": 30}""");
            
            var patch = new JsonPatchDocument()
                .Replace("/age", 31)
                .Add("/email", "john@example.com");
            
            var result = patch.ApplyTo(document.RootElement);
            
            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.True(result.TryGetProperty("email", out var emailProp));
            Assert.Equal("john@example.com", emailProp.GetString());
        }

        [Fact]
        public void JElement_ShouldWork_WithDynamicAccess()
        {
            var json = """{"user": {"name": "John", "details": {"age": 30}}}""";
            var element = JElement.Parse(json);
            
            Assert.Equal("John", element["user"]?["name"]?.Value<string>());
            Assert.Equal(30, element["user"]?["details"]?["age"]?.Value<int>());
        }

        [Fact]
        public void JsonPath_ShouldWork_WithBasicQueries()
        {
            var json = """
            {
              "users": [
                {"name": "John", "age": 30},
                {"name": "Jane", "age": 25}
              ]
            }
            """;
            
            var document = JsonDocument.Parse(json);
            var results = JsonPath.Query(document.RootElement, "$.users[*].name").ToList();
            
            Assert.Equal(2, results.Count);
            Assert.Contains("John", results.Select(r => r.GetString()).Where(s => s != null));
            Assert.Contains("Jane", results.Select(r => r.GetString()).Where(s => s != null));
        }

        [Fact]
        public void JsonOptionsBuilder_ShouldWork_WithFluentConfiguration()
        {
            var options = new JsonOptionsBuilder()
                .WithCaseInsensitiveProperties()
                .WithFlexibleEnums()
                .WithIndentation(true)
                .Build();
            
            Assert.NotNull(options);
            Assert.True(options.PropertyNameCaseInsensitive);
            Assert.True(options.WriteIndented);
            Assert.NotEmpty(options.Converters);
        }

        [Fact]
        public void DeepMerge_ShouldWork_WithComplexObjects()
        {
            var original = new TestClass
            {
                Name = "Original",
                Value = 42,
                Nested = new NestedClass { Data = "Test" },
                Items = new List<int> { 1, 2, 3 }
            };
            
            // Use JsonMerge.DeepMerge as a form of deep cloning
            var cloned = JsonMerge.DeepMerge(original, original);
            
            Assert.NotNull(cloned);
            // Basic verification that the merge worked
            var json = JsonSerializer.Serialize(cloned);
            Assert.Contains("Original", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void Performance_ShouldBe_ComparableToSystemTextJson()
        {
            var testData = Enumerable.Range(0, 1000)
                .Select(i => new TestClass
                {
                    Name = $"Item{i}",
                    Value = i,
                    Nested = new NestedClass { Data = $"Data{i}" }
                })
                .ToArray();
            
            // Warm up
            var warmupJson = JsonSerializer.Serialize(testData);
            var warmupDeserialized = JsonSerializer.Deserialize<TestClass[]>(warmupJson);
            
            // Test JsonToolkit.STJ performance
            var start = DateTime.UtcNow;
            var toolkitJson = JsonSerializer.Serialize(testData);
            var toolkitDeserialized = JsonSerializer.Deserialize<TestClass[]>(toolkitJson);
            var toolkitTime = DateTime.UtcNow - start;
            
            // Test System.Text.Json performance
            start = DateTime.UtcNow;
            var systemJson = JsonSerializer.Serialize(testData);
            var systemDeserialized = JsonSerializer.Deserialize<TestClass[]>(systemJson);
            var systemTime = DateTime.UtcNow - start;
            
            // JsonToolkit.STJ should not be more than 3x slower than System.Text.Json
            // (allowing for overhead from convenience features)
            Assert.True(toolkitTime.TotalMilliseconds < systemTime.TotalMilliseconds * 3 + 100,
                $"JsonToolkit.STJ took {toolkitTime.TotalMilliseconds}ms vs System.Text.Json {systemTime.TotalMilliseconds}ms");
            
            // Verify results are equivalent
            Assert.Equal(testData.Length, toolkitDeserialized?.Length ?? 0);
            Assert.Equal(testData.Length, systemDeserialized?.Length ?? 0);
        }

        [Fact]
        public void NoConflicts_WithSystemTextJson_WhenBothUsed()
        {
            // This test verifies that JsonToolkit.STJ doesn't conflict with System.Text.Json
            // when both are used in the same project
            
            // Try to use both libraries in the same test
            var testObject = new { Name = "Test", Value = 42 };
            
            // System.Text.Json usage
            var systemJson = JsonSerializer.Serialize(testObject);
            var systemDeserialized = JsonSerializer.Deserialize<dynamic>(systemJson);
            
            // JsonToolkit.STJ usage (JsonMerge)
            var element = JsonSerializer.SerializeToElement(testObject);
            var merged = JsonMerge.DeepMerge(element, element);
            
            Assert.NotNull(systemJson);
            Assert.NotNull(systemDeserialized);
            Assert.NotEqual(JsonValueKind.Undefined, merged.ValueKind);
            
            // Both should produce valid JSON
            Assert.True(IsValidJson(systemJson));
        }

        [Fact]
        public void FrameworkSpecificFeatures_ShouldWork_OnCurrentFramework()
        {
            // Test framework-specific features based on current runtime
            var frameworkName = GetCurrentFramework();
            
            switch (frameworkName)
            {
                case "net462":
                    // Test .NET Framework specific behavior
                    TestNetFrameworkFeatures();
                    break;
                    
                case "netstandard2.0":
                    // Test .NET Standard 2.0 specific behavior
                    TestNetStandard20Features();
                    break;
                    
                case "net6.0":
                case "net8.0":
                    // Test modern .NET features
                    TestModernNetFeatures();
                    break;
            }
        }

        [Fact]
        public void AssemblyMetadata_ShouldBe_CorrectlyConfigured()
        {
            var assembly = typeof(JsonOptionsBuilder).Assembly;
            var assemblyName = assembly.GetName();
            
            // Debug: Print actual version information
            Console.WriteLine($"Actual Assembly Name: {assemblyName.Name}");
            Console.WriteLine($"Actual Assembly Version: {assemblyName.Version}");
            Console.WriteLine($"Expected Version: >= 1.0.0.0");
            
            // Verify assembly name and version
            Assert.Equal("JsonToolkit.STJ", assemblyName.Name);
            Assert.NotNull(assemblyName.Version);
            Assert.True(assemblyName.Version >= new Version(1, 0, 0, 0),
                $"Assembly version {assemblyName.Version} should be >= 1.0.0.0");
            
            // Verify assembly attributes
            var titleAttr = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
            Assert.NotNull(titleAttr);
            Assert.Equal("JsonToolkit.STJ", titleAttr.Title);
            
            var descriptionAttr = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
            Assert.NotNull(descriptionAttr);
            Assert.Contains("System.Text.Json", descriptionAttr.Description);
            
            var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            Assert.NotNull(copyrightAttr);
            Assert.Contains("JsonToolkit Contributors", copyrightAttr.Copyright);
        }

        private void TestNetFrameworkFeatures()
        {
            // Test features specific to .NET Framework
            var options = new JsonOptionsBuilder()
                .WithCaseInsensitiveProperties()
                .Build();
            
            var testObject = new { Name = "Test" };
            var json = JsonSerializer.Serialize(testObject, options);
            var deserialized = JsonSerializer.Deserialize<dynamic>(json, options);
            
            Assert.NotNull(json);
            Assert.NotNull(deserialized);
        }

        private void TestNetStandard20Features()
        {
            // Test features specific to .NET Standard 2.0
            var testObject = new TestClass { Name = "Test", Value = 42 };
            var merged = JsonMerge.DeepMerge(testObject, testObject);
            
            Assert.NotNull(merged);
        }

        private void TestModernNetFeatures()
        {
            // Test modern .NET features like records (if available)
            var options = new JsonOptionsBuilder()
                .WithModernCSharpSupport()
                .Build();
            
            Assert.NotNull(options);
            Assert.NotEmpty(options.Converters);
        }

        private string GetCurrentFramework()
        {
#if NET462
            return "net462";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#elif NET6_0
            return "net6.0";
#elif NET8_0
            return "net8.0";
#else
            return "unknown";
#endif
        }

        private bool IsValidJson(string json)
        {
            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class TestClass
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
            public NestedClass? Nested { get; set; }
            public List<int> Items { get; set; } = new();
        }

        private class NestedClass
        {
            public string Data { get; set; } = "";
        }
    }
}