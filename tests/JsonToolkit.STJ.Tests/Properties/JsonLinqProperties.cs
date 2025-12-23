using System;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for LINQ-to-JSON functionality.
    /// </summary>
    public class JsonLinqProperties
    {
        /// <summary>
        /// Property 10: LINQ operations maintain query semantics.
        /// Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5
        /// </summary>
        [Property(MaxTest = 100)]
        public bool JsonLinq_WhereFiltersCorrectly(int[] values)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var threshold = values.Length > 0 ? values[0] : 0;
                var filtered = element.Where(e => e.GetInt32() > threshold).ToList();
                var expected = values.Where(v => v > threshold).Count();

                return filtered.Count == expected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_SelectProjectsCorrectly(int[] values)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var projected = element.Select(e => e.GetInt32() * 2).ToList();
                var expected = values.Select(v => v * 2).ToList();

                return projected.SequenceEqual(expected);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_FirstReturnsFirstElement(PositiveInt value1, int value2)
        {
            try
            {
                var values = new[] { value1.Get, value2 };
                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var first = element.First();

                return first.GetInt32() == value1.Get;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_CountReturnsCorrectCount(int[] values)
        {
            try
            {
                if (values == null) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var count = element.Count();

                return count == values.Length;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_SumComputesCorrectTotal(int[] values)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var sum = element.Sum();
                var expected = values.Sum(v => (double)v);

                return Math.Abs(sum - expected) < 0.001;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_AverageComputesCorrectValue(int[] values)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var avg = element.Average();
                var expected = values.Average(v => (double)v);

                return Math.Abs(avg - expected) < 0.001;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_AnyDetectsMatchingElements(int[] values, int target)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var hasMatch = element.Any(e => e.GetInt32() == target);
                var expected = values.Any(v => v == target);

                return hasMatch == expected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_AllChecksAllElements(PositiveInt threshold)
        {
            try
            {
                var values = new[] { threshold.Get + 1, threshold.Get + 2, threshold.Get + 3 };
                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var allAbove = element.All(e => e.GetInt32() > threshold.Get);

                return allAbove;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_ChainedOperationsMaintainSemantics(int[] values)
        {
            try
            {
                if (values == null || values.Length == 0) return true;

                var json = JsonSerializer.Serialize(values);
                var element = JsonDocument.Parse(json).RootElement;

                var threshold = values.Length > 0 ? values[0] : 0;
                var result = element
                    .Where(e => e.GetInt32() > threshold)
                    .Select(e => e.GetInt32() * 2)
                    .ToList();

                var expected = values
                    .Where(v => v > threshold)
                    .Select(v => v * 2)
                    .ToList();

                return result.SequenceEqual(expected);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool JsonLinq_NestedObjectFiltering(int value1, int value2)
        {
            try
            {
                var items = new[]
                {
                    new { Value = value1 },
                    new { Value = value2 }
                };
                var json = JsonSerializer.Serialize(items);
                var element = JsonDocument.Parse(json).RootElement;

                var threshold = Math.Min(value1, value2);
                var filtered = element.Where(e => 
                    e.TryGetProperty("Value", out var prop) && 
                    prop.GetInt32() > threshold
                ).ToList();

                var expected = items.Where(i => i.Value > threshold).Count();

                return filtered.Count == expected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public void JsonLinq_FirstThrowsOnEmptyArray()
        {
            var json = "[]";
            var element = JsonDocument.Parse(json).RootElement;

            Assert.Throws<InvalidOperationException>(() => element.First());
        }

        [Fact]
        public void JsonLinq_FirstOrDefaultReturnsNullOnEmpty()
        {
            var json = "[]";
            var element = JsonDocument.Parse(json).RootElement;

            var result = element.FirstOrDefault();

            Assert.Null(result);
        }
    }
}
