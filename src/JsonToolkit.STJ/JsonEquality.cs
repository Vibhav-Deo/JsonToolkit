namespace JsonToolkit.STJ;

public static class JsonEquality
{
    public static bool SemanticEquals(string json1, string json2, bool orderSensitive = false)
    {
        // Quick reference equality check
        if (ReferenceEquals(json1, json2)) return true;
        if (json1 == null || json2 == null) return false;
        
        // Quick string equality check for identical JSON
        if (json1 == json2) return true;
        
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        return SemanticEquals(doc1.RootElement, doc2.RootElement, orderSensitive);
    }

    public static bool SemanticEquals(JsonElement element1, JsonElement element2, bool orderSensitive = false)
    {
        // Quick value kind check - most common early exit
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
                // Optimized number comparison - avoid string allocation when possible
                return CompareNumbers(element1, element2);
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true; // Same ValueKind means they're equal
            default:
                return false;
        }
    }

    private static bool CompareNumbers(JsonElement num1, JsonElement num2)
    {
        // Try integer comparison first (most common case)
        if (num1.TryGetInt64(out var int1) && num2.TryGetInt64(out var int2))
            return int1 == int2;
            
        // Fall back to decimal comparison for precision
        if (num1.TryGetDecimal(out var dec1) && num2.TryGetDecimal(out var dec2))
            return dec1 == dec2;
            
        // Final fallback to double comparison
        return Math.Abs(num1.GetDouble() - num2.GetDouble()) < double.Epsilon;
    }

    private static bool CompareObjects(JsonElement obj1, JsonElement obj2, bool orderSensitive)
    {
        // Quick property count check
        var count1 = 0;
        var count2 = 0;
        
        foreach (var _ in obj1.EnumerateObject()) count1++;
        foreach (var _ in obj2.EnumerateObject()) count2++;
        
        if (count1 != count2) return false;
        
        // Early exit for empty objects
        if (count1 == 0) return true;

        // Use more efficient comparison for small objects
        if (count1 <= 5)
        {
            foreach (var prop1 in obj1.EnumerateObject())
            {
                if (!obj2.TryGetProperty(prop1.Name, out var prop2Value))
                    return false;
                if (!SemanticEquals(prop1.Value, prop2Value, orderSensitive))
                    return false;
            }
            return true;
        }

        // For larger objects, use dictionary approach
        var dict1 = new Dictionary<string, JsonElement>();
        var dict2 = new Dictionary<string, JsonElement>();

        foreach (var prop in obj1.EnumerateObject())
            dict1[prop.Name] = prop.Value;

        foreach (var prop in obj2.EnumerateObject())
            dict2[prop.Name] = prop.Value;

        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var value2)) return false;
            if (!SemanticEquals(kvp.Value, value2, orderSensitive)) return false;
        }

        return true;
    }

    private static bool CompareArrays(JsonElement arr1, JsonElement arr2, bool orderSensitive)
    {
        // Quick length check using GetArrayLength() when possible
        var length1 = arr1.GetArrayLength();
        var length2 = arr2.GetArrayLength();
        
        if (length1 != length2) return false;
        
        // Early exit for empty arrays
        if (length1 == 0) return true;

        if (orderSensitive)
        {
            // Optimized order-sensitive comparison
            using var enum1 = arr1.EnumerateArray().GetEnumerator();
            using var enum2 = arr2.EnumerateArray().GetEnumerator();
            
            while (enum1.MoveNext() && enum2.MoveNext())
            {
                if (!SemanticEquals(enum1.Current, enum2.Current, orderSensitive))
                    return false;
            }
            return true;
        }
        else
        {
            // Order-insensitive comparison - use efficient algorithm for all sizes
            var items1 = new List<JsonElement>(length1);
            var items2 = new List<JsonElement>(length2);

            foreach (var item in arr1.EnumerateArray())
                items1.Add(item);

            foreach (var item in arr2.EnumerateArray())
                items2.Add(item);

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
