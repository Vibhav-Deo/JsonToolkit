using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Provides JsonPath query capabilities for JSON documents.
    /// </summary>
    public static class JsonPath
    {
        // Cache for parsed path tokens to improve performance for repeated queries
        private static readonly ConcurrentDictionary<string, List<PathToken>> PathCache = new();
        
        // Simple object pool for List<JsonElement> to reduce allocations
        private static readonly ConcurrentQueue<List<JsonElement>> ListPool = new();
        private const int MaxPoolSize = 100;

        /// <summary>
        /// Queries a JSON document using a JsonPath expression and returns all matching elements.
        /// </summary>
        /// <param name="element">The JSON element to query.</param>
        /// <param name="path">The JsonPath expression.</param>
        /// <returns>An enumerable of matching JSON elements.</returns>
        public static IEnumerable<JsonElement> Query(JsonElement element, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new JsonPathException("JsonPath expression cannot be null or empty", path, 0);

            if (!path.StartsWith("$"))
                throw new JsonPathException("JsonPath expression must start with '$'", path, 0);

            // Use cached tokens for better performance
            var tokens = PathCache.GetOrAdd(path, ParsePath);
            return EvaluateTokens(new[] { element }, tokens);
        }

        /// <summary>
        /// Queries a JSON document and returns the first matching element, or null if no matches found.
        /// </summary>
        /// <param name="element">The JSON element to query.</param>
        /// <param name="path">The JsonPath expression.</param>
        /// <returns>The first matching element, or null if no matches found.</returns>
        public static JsonElement? QueryFirst(JsonElement element, string path)
        {
            var results = Query(element, path);
            using var enumerator = results.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        /// <summary>
        /// Queries a JSON document using a JsonPath expression and returns the first matching element as a strongly-typed object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the result to.</typeparam>
        /// <param name="element">The JSON element to query.</param>
        /// <param name="path">The JsonPath expression.</param>
        /// <param name="options">Optional JsonSerializerOptions.</param>
        /// <returns>The deserialized object, or default(T) if no matches found.</returns>
        public static T? QueryFirst<T>(JsonElement element, string path, JsonSerializerOptions? options = null)
        {
            var result = QueryFirst(element, path);
            return result.HasValue ? JsonSerializer.Deserialize<T>(result.Value, options) : default(T);
        }

        /// <summary>
        /// Queries a JSON document using a JsonPath expression and returns all matching elements as strongly-typed objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the results to.</typeparam>
        /// <param name="element">The JSON element to query.</param>
        /// <param name="path">The JsonPath expression.</param>
        /// <param name="options">Optional JsonSerializerOptions.</param>
        /// <returns>An enumerable of deserialized objects.</returns>
        public static IEnumerable<T> Query<T>(JsonElement element, string path, JsonSerializerOptions? options = null)
        {
            var results = Query(element, path);
            foreach (var result in results)
            {
                var obj = JsonSerializer.Deserialize<T>(result, options);
                if (obj != null)
                    yield return obj;
            }
        }

        /// <summary>
        /// Asynchronously queries a JSON document using a JsonPath expression and returns all matching elements as strongly-typed objects.
        /// Useful for large documents to avoid blocking the thread.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the results to.</typeparam>
        /// <param name="element">The JSON element to query.</param>
        /// <param name="path">The JsonPath expression.</param>
        /// <param name="options">Optional JsonSerializerOptions.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of deserialized objects.</returns>
        public static async IAsyncEnumerable<T> QueryAsync<T>(JsonElement element, string path, JsonSerializerOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var results = Query(element, path);
            foreach (var result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Yield control periodically for large result sets
                await Task.Yield();
                
                var obj = JsonSerializer.Deserialize<T>(result, options);
                if (obj != null)
                    yield return obj;
            }
        }

        private static List<JsonElement> GetPooledList()
        {
            if (ListPool.TryDequeue(out var list))
            {
                list.Clear();
                return list;
            }
            return new List<JsonElement>();
        }

        private static void ReturnPooledList(List<JsonElement> list)
        {
            if (ListPool.Count < MaxPoolSize)
            {
                list.Clear();
                ListPool.Enqueue(list);
            }
        }

        private static List<PathToken> ParsePath(string path)
        {
            var tokens = new List<PathToken>();
            var i = 1; // Skip '$'

            while (i < path.Length)
            {
                if (path[i] == '.')
                {
                    i++;
                    if (i < path.Length && path[i] == '.')
                    {
                        tokens.Add(new PathToken { Type = TokenType.RecursiveDescent });
                        i++;
                    }
                }
                else if (path[i] == '[')
                {
                    i++;
                    var closeBracket = path.IndexOf(']', i);
                    if (closeBracket == -1)
                        throw new JsonPathException("Unclosed bracket in JsonPath expression", path, i);

                    var contentLength = closeBracket - i;
                    var content = path.Substring(i, contentLength);
                    
                    if (content == "*")
                    {
                        tokens.Add(new PathToken { Type = TokenType.Wildcard });
                    }
                    else if (content.Length > 0 && content[0] == '?')
                    {
                        var filterContent = content.Substring(1);
                        // Remove parentheses if present - more efficient than Trim
                        if (filterContent.Length >= 2 && filterContent[0] == '(' && filterContent[filterContent.Length - 1] == ')')
                            filterContent = filterContent.Substring(1, filterContent.Length - 2);
                        tokens.Add(new PathToken { Type = TokenType.Filter, Filter = filterContent });
                    }
                    else if (int.TryParse(content, out var index))
                    {
                        tokens.Add(new PathToken { Type = TokenType.ArrayIndex, Index = index });
                    }
                    else if (content.Length >= 2 && content[0] == '\'' && content[content.Length - 1] == '\'')
                    {
                        tokens.Add(new PathToken { Type = TokenType.Property, Property = content.Substring(1, content.Length - 2) });
                    }
                    else
                    {
                        throw new JsonPathException($"Invalid bracket notation: [{content}]", path, i);
                    }

                    i = closeBracket + 1;
                }
                else if (path[i] == '*')
                {
                    tokens.Add(new PathToken { Type = TokenType.Wildcard });
                    i++;
                }
                else
                {
                    var end = i;
                    while (end < path.Length && path[end] != '.' && path[end] != '[')
                        end++;

                    if (end > i) // More efficient than checking string.IsNullOrEmpty
                    {
                        var property = path.Substring(i, end - i);
                        tokens.Add(new PathToken { Type = TokenType.Property, Property = property });
                    }

                    i = end;
                }
            }

            return tokens;
        }

        private static IEnumerable<JsonElement> EvaluateTokens(IEnumerable<JsonElement> elements, List<PathToken> tokens)
        {
            var current = elements;

            foreach (var token in tokens)
            {
                current = token.Type switch
                {
                    TokenType.Property => EvaluateProperty(current, token.Property!),
                    TokenType.ArrayIndex => EvaluateArrayIndex(current, token.Index),
                    TokenType.Wildcard => EvaluateWildcard(current),
                    TokenType.RecursiveDescent => EvaluateRecursiveDescent(current),
                    TokenType.Filter => EvaluateFilter(current, token.Filter!),
                    _ => current
                };
            }

            return current;
        }

        private static IEnumerable<JsonElement> EvaluateProperty(IEnumerable<JsonElement> elements, string property)
        {
            foreach (var element in elements)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value))
                    yield return value;
            }
        }

        private static IEnumerable<JsonElement> EvaluateArrayIndex(IEnumerable<JsonElement> elements, int index)
        {
            foreach (var element in elements)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    var array = GetPooledList();
                    try
                    {
                        array.AddRange(element.EnumerateArray());
                        var actualIndex = index < 0 ? array.Count + index : index;
                        if (actualIndex >= 0 && actualIndex < array.Count)
                            yield return array[actualIndex];
                    }
                    finally
                    {
                        ReturnPooledList(array);
                    }
                }
            }
        }

        private static IEnumerable<JsonElement> EvaluateWildcard(IEnumerable<JsonElement> elements)
        {
            foreach (var element in elements)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                        yield return property.Value;
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                        yield return item;
                }
            }
        }

        private static IEnumerable<JsonElement> EvaluateRecursiveDescent(IEnumerable<JsonElement> elements)
        {
            foreach (var element in elements)
            {
                yield return element;
                foreach (var descendant in GetDescendants(element))
                    yield return descendant;
            }
        }

        private static IEnumerable<JsonElement> GetDescendants(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Value;
                    foreach (var descendant in GetDescendants(property.Value))
                        yield return descendant;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    yield return item;
                    foreach (var descendant in GetDescendants(item))
                        yield return descendant;
                }
            }
        }

        private static IEnumerable<JsonElement> EvaluateFilter(IEnumerable<JsonElement> elements, string filter)
        {
            foreach (var element in elements)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        if (EvaluateFilterExpression(item, filter))
                            yield return item;
                    }
                }
            }
        }

        private static bool EvaluateFilterExpression(JsonElement element, string filter)
        {
            // Simple filter: @.property == value or @.property > value
            var parts = filter.Split(new[] { "==", "!=", ">", "<", ">=", "<=" }, StringSplitOptions.None);
            if (parts.Length != 2)
                return false;

            var propertyPath = parts[0].Trim().TrimStart('@', '.');
            var expectedValue = parts[1].Trim().Trim('\'', '"');

            var op = filter.Contains("==") ? "==" :
                     filter.Contains("!=") ? "!=" :
                     filter.Contains(">=") ? ">=" :
                     filter.Contains("<=") ? "<=" :
                     filter.Contains(">") ? ">" :
                     filter.Contains("<") ? "<" : null;

            if (op == null || !element.TryGetProperty(propertyPath, out var property))
                return false;

            var actualValue = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };

            if (actualValue == null)
                return false;

            return op switch
            {
                "==" => actualValue == expectedValue,
                "!=" => actualValue != expectedValue,
                ">" => double.TryParse(actualValue, out var a) && double.TryParse(expectedValue, out var b) && a > b,
                "<" => double.TryParse(actualValue, out var a) && double.TryParse(expectedValue, out var b) && a < b,
                ">=" => double.TryParse(actualValue, out var a) && double.TryParse(expectedValue, out var b) && a >= b,
                "<=" => double.TryParse(actualValue, out var a) && double.TryParse(expectedValue, out var b) && a <= b,
                _ => false
            };
        }

        private enum TokenType
        {
            Property,
            ArrayIndex,
            Wildcard,
            RecursiveDescent,
            Filter
        }

        private class PathToken
        {
            public TokenType Type { get; set; }
            public string? Property { get; set; }
            public int Index { get; set; }
            public string? Filter { get; set; }
        }
    }
}
