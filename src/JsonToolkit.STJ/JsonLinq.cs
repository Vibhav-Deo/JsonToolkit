using System.Linq;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Provides LINQ-style extension methods for querying JSON documents.
    /// </summary>
    public static class JsonLinq
    {
        /// <summary>
        /// Filters JSON array elements based on a predicate.
        /// </summary>
        public static IEnumerable<JsonElement> Where(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                yield break;

            foreach (var item in element.EnumerateArray())
            {
                if (predicate(item))
                    yield return item;
            }
        }

        /// <summary>
        /// Projects each JSON element to a new form.
        /// </summary>
        public static IEnumerable<TResult> Select<TResult>(this JsonElement element, Func<JsonElement, TResult> selector)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    yield return selector(item);
            }
        }

        /// <summary>
        /// Returns the first element of a JSON array.
        /// </summary>
        public static JsonElement First(this JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Element is not an array");

            using var enumerator = element.EnumerateArray().GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Array is empty");

            return enumerator.Current;
        }

        /// <summary>
        /// Returns the first element that satisfies a condition.
        /// </summary>
        public static JsonElement First(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Element is not an array");

            foreach (var item in element.EnumerateArray())
            {
                if (predicate(item))
                    return item;
            }

            throw new InvalidOperationException("No element satisfies the condition");
        }

        /// <summary>
        /// Returns the first element or a default value if empty.
        /// </summary>
        public static JsonElement? FirstOrDefault(this JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return null;

            using var enumerator = element.EnumerateArray().GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        /// <summary>
        /// Returns the first element that satisfies a condition or a default value.
        /// </summary>
        public static JsonElement? FirstOrDefault(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in element.EnumerateArray())
            {
                if (predicate(item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Returns the number of elements in a JSON array.
        /// </summary>
        public static int Count(this JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            return element.GetArrayLength();
        }

        /// <summary>
        /// Returns the number of elements that satisfy a condition.
        /// </summary>
        public static int Count(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            return element.EnumerateArray().Count(predicate);
        }

        /// <summary>
        /// Computes the sum of numeric values in a JSON array.
        /// </summary>
        public static double Sum(this JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            double sum = 0;
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                    sum += item.GetDouble();
            }

            return sum;
        }

        /// <summary>
        /// Computes the sum of values selected from JSON elements.
        /// </summary>
        public static double Sum(this JsonElement element, Func<JsonElement, double> selector)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            return element.EnumerateArray().Sum(selector);
        }

        /// <summary>
        /// Computes the average of numeric values in a JSON array.
        /// </summary>
        public static double Average(this JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            double sum = 0;
            int count = 0;
            
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                {
                    sum += item.GetDouble();
                    count++;
                }
            }

            return count == 0 ? 0 : sum / count;
        }

        /// <summary>
        /// Computes the average of values selected from JSON elements.
        /// </summary>
        public static double Average(this JsonElement element, Func<JsonElement, double> selector)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return 0;

            double sum = 0;
            int count = 0;
            
            foreach (var item in element.EnumerateArray())
            {
                sum += selector(item);
                count++;
            }

            return count == 0 ? 0 : sum / count;
        }

        /// <summary>
        /// Determines whether any element satisfies a condition.
        /// </summary>
        public static bool Any(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return false;

            return element.EnumerateArray().Any(predicate);
        }

        /// <summary>
        /// Determines whether all elements satisfy a condition.
        /// </summary>
        public static bool All(this JsonElement element, Func<JsonElement, bool> predicate)
        {
            if (element.ValueKind != JsonValueKind.Array)
                return true;

            return element.EnumerateArray().All(predicate);
        }
    }
}
