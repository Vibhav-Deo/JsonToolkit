using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonToolkit.STJ;

public static class JsonEquality
{
    public static bool SemanticEquals(string json1, string json2, bool orderSensitive = false)
    {
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        return SemanticEquals(doc1.RootElement, doc2.RootElement, orderSensitive);
    }

    public static bool SemanticEquals(JsonElement element1, JsonElement element2, bool orderSensitive = false)
    {
        if (element1.ValueKind != element2.ValueKind) return false;

        switch (element1.ValueKind)
        {
            case JsonValueKind.Object:
                return CompareObjects(element1, element2, orderSensitive);
            case JsonValueKind.Array:
                return CompareArrays(element1, element2, orderSensitive);
            case JsonValueKind.String:
                return element1.GetString() == element2.GetString();
            case JsonValueKind.Number:
                return element1.GetRawText() == element2.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true;
            default:
                return false;
        }
    }

    private static bool CompareObjects(JsonElement obj1, JsonElement obj2, bool orderSensitive)
    {
        var dict1 = new Dictionary<string, JsonElement>();
        var dict2 = new Dictionary<string, JsonElement>();

        foreach (var prop in obj1.EnumerateObject())
            dict1[prop.Name] = prop.Value;

        foreach (var prop in obj2.EnumerateObject())
            dict2[prop.Name] = prop.Value;

        if (dict1.Count != dict2.Count) return false;

        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var value2)) return false;
            if (!SemanticEquals(kvp.Value, value2, orderSensitive)) return false;
        }

        return true;
    }

    private static bool CompareArrays(JsonElement arr1, JsonElement arr2, bool orderSensitive)
    {
        var items1 = new List<JsonElement>();
        var items2 = new List<JsonElement>();

        foreach (var item in arr1.EnumerateArray())
            items1.Add(item);

        foreach (var item in arr2.EnumerateArray())
            items2.Add(item);

        if (items1.Count != items2.Count) return false;

        if (orderSensitive)
        {
            for (int i = 0; i < items1.Count; i++)
            {
                if (!SemanticEquals(items1[i], items2[i], orderSensitive)) return false;
            }
        }
        else
        {
            var matched = new bool[items2.Count];
            foreach (var item1 in items1)
            {
                bool found = false;
                for (int i = 0; i < items2.Count; i++)
                {
                    if (!matched[i] && SemanticEquals(item1, items2[i], orderSensitive))
                    {
                        matched[i] = true;
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
        }

        return true;
    }
}
