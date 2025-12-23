using System;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for JsonPath query functionality.
    /// </summary>
    public class JsonPathProperties
    {
        /// <summary>
        /// Property 9: JsonPath queries return correct matches.
        /// Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5
        /// </summary>
        [Property(MaxTest = 100)]
        public bool JsonPath_PropertyAccessReturnsCorrectValue(int value)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { Property = value });
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, "$.Property").ToList();

                return results.Count == 1 && results[0].GetInt32() == value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_WildcardReturnsAllProperties(int value1, int value2, int value3)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { A = value1, B = value2, C = value3 });
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, "$.*").ToList();

                return results.Count == 3;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_ArrayIndexReturnsCorrectElement(PositiveInt index)
        {
            try
            {
                var array = Enumerable.Range(0, Math.Max(10, index.Get + 1)).ToArray();
                var json = JsonSerializer.Serialize(array);
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, $"$[{index.Get}]").ToList();

                return results.Count == 1 && results[0].GetInt32() == index.Get;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_RecursiveDescentFindsNestedProperties(int value)
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    Level1 = new
                    {
                        Target = value,
                        Level2 = new
                        {
                            Target = value * 2
                        }
                    }
                });
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, "$..Target").ToList();

                return results.Count == 2 && 
                       results[0].GetInt32() == value && 
                       results[1].GetInt32() == value * 2;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_FilterExpressionReturnsMatchingElements(PositiveInt threshold)
        {
            try
            {
                var items = new[]
                {
                    new { Value = threshold.Get - 1 },
                    new { Value = threshold.Get },
                    new { Value = threshold.Get + 1 }
                };
                var json = JsonSerializer.Serialize(items);
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, $"$[?(@.Value > {threshold.Get})]").ToList();

                return results.Count == 1 && results[0].GetProperty("Value").GetInt32() == threshold.Get + 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_NoMatchesReturnsEmptyResult(string propertyName)
        {
            try
            {
                if (propertyName == null) return true;
                var safeName = new string(propertyName.Where(c => !char.IsControl(c) && c != '"' && c != '\\' && c != '[' && c != ']' && c != '.' && c != '*' && c != '$').ToArray());
                if (string.IsNullOrWhiteSpace(safeName))
                    return true;

                var json = JsonSerializer.Serialize(new { OtherProperty = 42 });
                var element = JsonDocument.Parse(json).RootElement;

                var results = JsonPath.Query(element, $"$.{safeName}").ToList();

                return results.Count == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public void JsonPath_InvalidExpressionThrowsDescriptiveError()
        {
            var json = JsonSerializer.Serialize(new { Property = 42 });
            var element = JsonDocument.Parse(json).RootElement;

            var ex = Assert.Throws<JsonPathException>(() => JsonPath.Query(element, "invalid").ToList());
            Assert.Contains("must start with '$'", ex.Message);
        }

        [Fact]
        public void JsonPath_UnclosedBracketThrowsDescriptiveError()
        {
            var json = JsonSerializer.Serialize(new { Property = 42 });
            var element = JsonDocument.Parse(json).RootElement;

            var ex = Assert.Throws<JsonPathException>(() => JsonPath.Query(element, "$[0").ToList());
            Assert.Contains("Unclosed bracket", ex.Message);
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_QueryFirstReturnsFirstMatch(int value1, int value2)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { A = value1, B = value2 });
                var element = JsonDocument.Parse(json).RootElement;

                var result = JsonPath.QueryFirst(element, "$.*");

                return result.HasValue && result.Value.GetInt32() == value1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonPath_QueryFirstReturnsNullWhenNoMatch(string propertyName)
        {
            try
            {
                if (propertyName == null) return true;
                var safeName = new string(propertyName.Where(c => !char.IsControl(c) && c != '"' && c != '\\' && c != '[' && c != ']' && c != '.' && c != '*' && c != '$').ToArray());
                if (string.IsNullOrWhiteSpace(safeName))
                    return true;

                var json = JsonSerializer.Serialize(new { OtherProperty = 42 });
                var element = JsonDocument.Parse(json).RootElement;

                var result = JsonPath.QueryFirst(element, $"$.{safeName}");

                return !result.HasValue;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
