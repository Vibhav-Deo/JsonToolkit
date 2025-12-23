using System.Linq;

namespace JsonToolkit.STJ;

/// <summary>
/// Wrapper around JsonElement providing Newtonsoft.Json-like dynamic access.
/// </summary>
public class JElement
{
        private readonly JsonElement _element;

        private JElement(JsonElement element)
        {
            _element = element;
        }

        /// <summary>
        /// Gets the underlying JsonElement.
        /// </summary>
        public JsonElement Element => _element;

        /// <summary>
        /// Gets the value kind of this element.
        /// </summary>
        public JsonValueKind ValueKind => _element.ValueKind;

        /// <summary>
        /// Parses JSON text into a JElement.
        /// </summary>
        public static JElement Parse(string json)
        {
            var doc = JsonDocument.Parse(json);
            return new JElement(doc.RootElement.Clone());
        }

        /// <summary>
        /// Creates a JElement from an object.
        /// </summary>
        public static JElement FromObject(object obj, JsonSerializerOptions? options = null)
        {
            var json = JsonSerializer.Serialize(obj, options);
            return Parse(json);
        }

        /// <summary>
        /// Indexer for property access.
        /// </summary>
        public JElement? this[string propertyName]
        {
            get
            {
                if (_element.ValueKind != JsonValueKind.Object)
                    return null;

                return _element.TryGetProperty(propertyName, out var prop)
                    ? new JElement(prop)
                    : null;
            }
        }

        /// <summary>
        /// Indexer for array access.
        /// </summary>
        public JElement? this[int index]
        {
            get
            {
                if (_element.ValueKind != JsonValueKind.Array)
                    return null;

                if (index < 0)
                    return null;

                var arrayEnumerator = _element.EnumerateArray();
                var currentIndex = 0;
                foreach (var item in arrayEnumerator)
                {
                    if (currentIndex == index)
                        return new JElement(item);
                    currentIndex++;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the value as the specified type.
        /// </summary>
        public T? Value<T>()
        {
            return JsonSerializer.Deserialize<T>(_element.GetRawText());
        }

        /// <summary>
        /// Converts this element to an object of the specified type.
        /// </summary>
        public T? ToObject<T>(JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<T>(_element.GetRawText(), options);
        }

        /// <summary>
        /// Gets all child elements (for objects and arrays).
        /// </summary>
        public IEnumerable<JElement> Children()
        {
            return _element.ValueKind switch
            {
                JsonValueKind.Object => _element.EnumerateObject().Select(p => new JElement(p.Value)),
                JsonValueKind.Array => _element.EnumerateArray().Select(e => new JElement(e)),
                _ => Enumerable.Empty<JElement>()
            };
        }

        /// <summary>
        /// Gets all descendant elements recursively.
        /// </summary>
        public IEnumerable<JElement> Descendants()
        {
            foreach (var child in Children())
            {
                yield return child;
                foreach (var descendant in child.Descendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Returns the JSON string representation.
        /// </summary>
        public override string ToString()
        {
            return _element.GetRawText();
        }

        /// <summary>
        /// Implicit conversion from JsonElement.
        /// </summary>
        public static implicit operator JElement(JsonElement element)
        {
            return new JElement(element);
        }

        /// <summary>
        /// Implicit conversion to JsonElement.
        /// </summary>
        public static implicit operator JsonElement(JElement jElement)
        {
            return jElement._element;
        }
    }
