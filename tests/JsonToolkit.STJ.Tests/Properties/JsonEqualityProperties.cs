using FsCheck;
using FsCheck.Xunit;
using JsonToolkit.STJ.Extensions;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties;

public class JsonEqualityProperties
{
    [Property]
    public Property SemanticEquals_IgnoresPropertyOrder()
    {
        return Prop.ForAll<string, int>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            Arb.From<int>(),
            (name, value) =>
            {
                var json1 = JsonSerializer.Serialize(new { Name = name, Value = value });
                var json2 = JsonSerializer.Serialize(new { Value = value, Name = name });

                return json1.SemanticEquals(json2).Label("Property order should not affect equality");
            });
    }

    [Property]
    public Property SemanticEquals_IgnoresWhitespace()
    {
        return Prop.ForAll(
            Arb.From<int>(),
            value =>
            {
                var json1 = JsonSerializer.Serialize(new { Value = value });
                var json2 = JsonSerializer.Serialize(new { Value = value }, new JsonSerializerOptions { WriteIndented = true });

                return json1.SemanticEquals(json2).Label("Whitespace should not affect equality");
            });
    }

    [Property]
    public Property SemanticEquals_DetectsDifferentValues()
    {
        return Prop.ForAll(
            Arb.From<int>(),
            Arb.From<int>(),
            (value1, value2) =>
            {
                if (value1 == value2) return true.ToProperty();

                var json1 = JsonSerializer.Serialize(new { Value = value1 });
                var json2 = JsonSerializer.Serialize(new { Value = value2 });

                return (!json1.SemanticEquals(json2)).Label("Different values should not be equal");
            });
    }

    [Property]
    public Property SemanticEquals_HandlesNestedObjects()
    {
        return Prop.ForAll<string, int>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            Arb.From<int>(),
            (name, value) =>
            {
                var json1 = JsonSerializer.Serialize(new { Outer = new { Name = name, Value = value } });
                var json2 = JsonSerializer.Serialize(new { Outer = new { Value = value, Name = name } });

                return json1.SemanticEquals(json2).Label("Nested object property order should not affect equality");
            });
    }

    [Property]
    public Property SemanticEquals_ArrayOrderSensitive()
    {
        return Prop.ForAll<int[]>(
            Arb.Default.Array<int>().Generator.Where(arr => arr != null && arr.Length > 1 && arr.Distinct().Count() == arr.Length).ToArbitrary(),
            arr =>
            {
                var reversed = arr.Reverse().ToArray();
                var json1 = JsonSerializer.Serialize(arr);
                var json2 = JsonSerializer.Serialize(reversed);

                return (!json1.SemanticEquals(json2, orderSensitive: true)).Label("Array order should matter when orderSensitive=true");
            });
    }

    [Property]
    public Property SemanticEquals_ArrayOrderInsensitive()
    {
        return Prop.ForAll<int[]>(
            Arb.Default.Array<int>().Generator.Where(arr => arr != null && arr.Length > 0).ToArbitrary(),
            arr =>
            {
                var shuffled = arr.OrderByDescending(x => x).ToArray();
                var json1 = JsonSerializer.Serialize(arr);
                var json2 = JsonSerializer.Serialize(shuffled);

                return json1.SemanticEquals(json2, orderSensitive: false).Label("Array order should not matter when orderSensitive=false");
            });
    }

    [Property]
    public Property SemanticEquals_IsReflexive()
    {
        return Prop.ForAll<string, int>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            Arb.From<int>(),
            (name, value) =>
            {
                var json = JsonSerializer.Serialize(new { Name = name, Value = value });

                return json.SemanticEquals(json).Label("JSON should equal itself");
            });
    }

    [Property]
    public Property SemanticEquals_IsSymmetric()
    {
        return Prop.ForAll(
            Arb.From<int>(),
            value =>
            {
                var json1 = JsonSerializer.Serialize(new { Value = value });
                var json2 = JsonSerializer.Serialize(new { Value = value }, new JsonSerializerOptions { WriteIndented = true });

                var equals1 = json1.SemanticEquals(json2);
                var equals2 = json2.SemanticEquals(json1);

                return (equals1 == equals2).Label("Equality should be symmetric");
            });
    }

    [Property]
    public Property SemanticEquals_DetectsMissingProperties()
    {
        return Prop.ForAll(
            Arb.From<int>(),
            value =>
            {
                var json1 = JsonSerializer.Serialize(new { Value = value, Extra = "test" });
                var json2 = JsonSerializer.Serialize(new { Value = value });

                return (!json1.SemanticEquals(json2)).Label("Missing properties should cause inequality");
            });
    }

    [Property]
    public Property SemanticEquals_HandlesNullValues()
    {
        return Prop.ForAll(
            Arb.From<int>(),
            value =>
            {
                var json1 = JsonSerializer.Serialize(new { Value = value, Name = (string?)null });
                var json2 = JsonSerializer.Serialize(new { Value = value, Name = (string?)null });

                return json1.SemanticEquals(json2).Label("Null values should be handled correctly");
            });
    }
}
