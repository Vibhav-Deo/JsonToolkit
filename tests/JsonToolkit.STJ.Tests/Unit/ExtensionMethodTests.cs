using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using JsonToolkit.STJ.Extensions;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for extension methods to debug the property test failures.
    /// </summary>
    public class ExtensionMethodTests
    {
        [Fact]
        public void ToJson_SimpleObject_ShouldSerialize()
        {
            var obj = new { Name = "Test", Value = 42 };
            var json = obj.ToJson();
            
            Assert.NotNull(json);
            Assert.Contains("Test", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void FromJson_SimpleObject_ShouldDeserialize()
        {
            var json = """{"Name":"Test","Value":42}""";
            var obj = json.FromJson<TestClass>();
            
            Assert.NotNull(obj);
            Assert.Equal("Test", obj.Name);
            Assert.Equal(42, obj.Value);
        }

        [Fact]
        public void RoundTrip_SimpleObject_ShouldPreserveValues()
        {
            var original = new TestClass { Name = "Test", Value = 42 };
            var json = original.ToJson();
            var roundTrip = json.FromJson<TestClass>();
            
            Assert.Equal(original.Name, roundTrip.Name);
            Assert.Equal(original.Value, roundTrip.Value);
        }

        [Fact]
        public void DeepClone_SimpleObject_ShouldCreateNewInstance()
        {
            var original = new TestClass { Name = "Test", Value = 42 };
            var cloned = original.DeepClone();
            
            Assert.NotSame(original, cloned);
            Assert.Equal(original.Name, cloned.Name);
            Assert.Equal(original.Value, cloned.Value);
        }

        [Fact]
        public void ToJsonBytes_SimpleObject_ShouldSerializeToBytes()
        {
            var obj = new { Name = "Test", Value = 42 };
            var jsonBytes = obj.ToJsonBytes();
            var json = Encoding.UTF8.GetString(jsonBytes);
            
            Assert.NotNull(jsonBytes);
            Assert.True(jsonBytes.Length > 0);
            Assert.Contains("Test", json);
        }

        [Fact]
        public async Task StreamExtensions_ShouldWorkCorrectly()
        {
            var original = new TestClass { Name = "Test", Value = 42 };
            
            using var stream = new MemoryStream();
            await original.ToJsonAsync(stream);
            
            stream.Position = 0;
            var roundTrip = await stream.FromJsonAsync<TestClass>();
            
            Assert.Equal(original.Name, roundTrip.Name);
            Assert.Equal(original.Value, roundTrip.Value);
        }

        [Fact]
        public void NullHandling_ShouldWorkCorrectly()
        {
            TestClass? nullObj = null;
            var json = nullObj.ToJson();
            var roundTrip = json.FromJson<TestClass?>();
            var cloned = nullObj.DeepClone();
            
            Assert.Equal("null", json);
            Assert.Null(roundTrip);
            Assert.Null(cloned);
        }

        [Fact]
        public void ComplexObject_ShouldRoundTripCorrectly()
        {
            var original = new ComplexTestClass
            {
                StringValue = "Test",
                IntValue = 42,
                BoolValue = true,
                DoubleValue = 3.14,
                DateTimeValue = new DateTime(2023, 1, 1),
                ListValue = new List<string> { "a", "b", "c" },
                DictionaryValue = new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } },
                NestedObject = new NestedClass { Name = "Nested", Value = 100 }
            };

            var json = original.ToJson();
            var roundTrip = json.FromJson<ComplexTestClass>();

            Assert.Equal(original.StringValue, roundTrip.StringValue);
            Assert.Equal(original.IntValue, roundTrip.IntValue);
            Assert.Equal(original.BoolValue, roundTrip.BoolValue);
            Assert.Equal(original.DoubleValue, roundTrip.DoubleValue);
            Assert.Equal(original.DateTimeValue, roundTrip.DateTimeValue);
            Assert.Equal(original.ListValue, roundTrip.ListValue);
            Assert.Equal(original.DictionaryValue, roundTrip.DictionaryValue);
            Assert.Equal(original.NestedObject.Name, roundTrip.NestedObject.Name);
            Assert.Equal(original.NestedObject.Value, roundTrip.NestedObject.Value);
        }
    }

    public class TestClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class ComplexTestClass
    {
        public string? StringValue { get; set; }
        public int IntValue { get; set; }
        public bool BoolValue { get; set; }
        public double DoubleValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public List<string>? ListValue { get; set; }
        public Dictionary<string, int>? DictionaryValue { get; set; }
        public NestedClass? NestedObject { get; set; }
    }

    public class NestedClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}