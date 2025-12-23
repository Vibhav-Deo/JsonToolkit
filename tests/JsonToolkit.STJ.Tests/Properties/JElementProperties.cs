using System;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for JElement dynamic JSON access.
    /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
    /// </summary>
    public class JElementProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Parsing and serializing should preserve structure.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool JElement_ParseShouldPreserveStructure(string name, int value)
        {
            try
            {
                if (name == null) return true;
                foreach (var c in name)
                {
                    if (char.IsControl(c) || c == '"' || c == '\\') return true;
                }

                var json = $"{{\"Name\":\"{name}\",\"Value\":{value}}}";
                var element = JElement.Parse(json);

                return element["Name"]?.Value<string>() == name &&
                       element["Value"]?.Value<int>() == value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// FromObject should create valid JElement.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool JElement_FromObjectShouldWork(string data, int count)
        {
            try
            {
                if (data == null) return true;

                var obj = new { Data = data, Count = count };
                var element = JElement.FromObject(obj);

                return element["Data"]?.Value<string>() == data &&
                       element["Count"]?.Value<int>() == count;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Array indexing should work correctly.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool JElement_ArrayIndexingShouldWork(int a, int b, int c)
        {
            try
            {
                var json = $"[{a},{b},{c}]";
                var element = JElement.Parse(json);

                return element[0]?.Value<int>() == a &&
                       element[1]?.Value<int>() == b &&
                       element[2]?.Value<int>() == c;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Children enumeration should return all child elements.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool JElement_ChildrenShouldEnumerateCorrectly(int count)
        {
            try
            {
                if (count < 0 || count > 10) return true;

                var items = Enumerable.Range(0, count).Select(i => i.ToString());
                var json = "[" + string.Join(",", items) + "]";
                var element = JElement.Parse(json);

                var children = element.Children().ToList();
                return children.Count == count;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// ToObject should deserialize correctly.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool JElement_ToObjectShouldWork(string name, int value)
        {
            try
            {
                if (name == null) return true;
                foreach (var c in name)
                {
                    if (char.IsControl(c) || c == '"' || c == '\\') return true;
                }

                var json = $"{{\"Name\":\"{name}\",\"Value\":{value}}}";
                var element = JElement.Parse(json);
                var obj = element.ToObject<JElementTestObject>();

                return obj != null && obj.Name == name && obj.Value == value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 6: Dynamic JSON access preserves structure**
        /// Nested property access should work.
        /// **Validates: Requirements 5.3, 5.4**
        /// </summary>
        [Property(MaxTest = 50)]
        public bool JElement_NestedAccessShouldWork(string innerValue)
        {
            try
            {
                if (innerValue == null) return true;
                foreach (var c in innerValue)
                {
                    if (char.IsControl(c) || c == '"' || c == '\\') return true;
                }

                var json = $"{{\"Outer\":{{\"Inner\":\"{innerValue}\"}}}}";
                var element = JElement.Parse(json);

                return element["Outer"]?["Inner"]?.Value<string>() == innerValue;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class JElementTestObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
