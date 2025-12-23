using System;
using System.Reflection;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Integration
{
    /// <summary>
    /// Tests to verify NuGet package configuration and metadata.
    /// </summary>
    public class PackageConfigurationTests
    {
        [Fact]
        public void Assembly_ShouldHave_CorrectMetadata()
        {
            var assembly = typeof(JsonOptionsBuilder).Assembly;
            var assemblyName = assembly.GetName();
            
            // Verify assembly name and version
            Assert.Equal("JsonToolkit.STJ", assemblyName.Name);
            Assert.NotNull(assemblyName.Version);
            Assert.True(assemblyName.Version >= new Version(1, 0, 0, 0), 
                $"Assembly version {assemblyName.Version} should be >= 1.0.0.0");
        }

        [Fact]
        public void Assembly_ShouldHave_CorrectAttributes()
        {
            var assembly = typeof(JsonOptionsBuilder).Assembly;
            
            // Verify assembly title
            var titleAttr = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
            Assert.NotNull(titleAttr);
            Assert.Equal("JsonToolkit.STJ", titleAttr.Title);
            
            // Verify assembly description
            var descriptionAttr = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
            Assert.NotNull(descriptionAttr);
            Assert.Contains("System.Text.Json", descriptionAttr.Description);
            
            // Verify copyright
            var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            Assert.NotNull(copyrightAttr);
            Assert.Contains("JsonToolkit Contributors", copyrightAttr.Copyright);
        }

        [Fact]
        public void Package_ShouldLoad_WithoutErrors()
        {
            // Verify the package loads without throwing exceptions
            var assembly = typeof(JsonOptionsBuilder).Assembly;
            Assert.NotNull(assembly);
            
            // Verify key types are available and public
            var keyTypes = new[]
            {
                typeof(JsonOptionsBuilder),
                typeof(JsonMerge),
                typeof(JElement),
                typeof(JsonPatchDocument),
                typeof(JsonPath)
            };
            
            foreach (var type in keyTypes)
            {
                Assert.NotNull(type);
                Assert.True(type.IsPublic, $"Type {type.Name} should be public");
            }
        }

        [Fact]
        public void SystemTextJson_ShouldBe_Available()
        {
            // Verify System.Text.Json is available and working
            var testObject = new { Name = "Test", Value = 42 };
            var json = JsonSerializer.Serialize(testObject);
            
            Assert.NotNull(json);
            Assert.Contains("Test", json);
            Assert.Contains("42", json);
            
            var deserialized = JsonSerializer.Deserialize<dynamic>(json);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void JsonOptionsBuilder_ShouldWork_WithBasicConfiguration()
        {
            // Test that the main configuration class works
            var options = new JsonOptionsBuilder()
                .WithCaseInsensitiveProperties()
                .WithIndentation()
                .Build();
            
            Assert.NotNull(options);
            Assert.True(options.PropertyNameCaseInsensitive);
            Assert.True(options.WriteIndented);
        }

        [Fact]
        public void JsonMerge_ShouldWork_WithBasicMerge()
        {
            // Test that the main functionality works
            var json1 = """{"A": 1, "B": 2}""";
            var json2 = """{"B": 3, "C": 4}""";
            
            var element1 = JsonDocument.Parse(json1).RootElement;
            var element2 = JsonDocument.Parse(json2).RootElement;
            
            var merged = JsonMerge.DeepMerge(element1, element2);
            
            Assert.NotEqual(JsonValueKind.Undefined, merged.ValueKind);
            Assert.True(merged.TryGetProperty("A", out var aProp));
            Assert.Equal(1, aProp.GetInt32());
            Assert.True(merged.TryGetProperty("C", out var cProp));
            Assert.Equal(4, cProp.GetInt32());
        }

        [Fact]
        public void CurrentFramework_ShouldBe_Supported()
        {
            // Verify we're running on a supported framework
            var frameworkName = GetCurrentFramework();
            var supportedFrameworks = new[] { "net462", "netstandard2.0", "net6.0", "net8.0", "net9.0" };
            
            Assert.Contains(frameworkName, supportedFrameworks);
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
#elif NET9_0
            return "net9.0";
#else
            return "unknown";
#endif
        }
    }
}