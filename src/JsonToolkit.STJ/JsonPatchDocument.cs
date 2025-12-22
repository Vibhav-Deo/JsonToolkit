using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Represents a JSON Patch document that can be applied to JSON documents.
    /// Supports RFC 6902 JSON Patch operations: add, remove, replace, move, copy, test.
    /// </summary>
    public class JsonPatchDocument
    {
        private readonly List<JsonPatchOperation> _operations = new();

        /// <summary>
        /// Gets the list of operations in this patch document.
        /// </summary>
        public IReadOnlyList<JsonPatchOperation> Operations => _operations.AsReadOnly();

        /// <summary>
        /// Adds an "add" operation to the patch document.
        /// </summary>
        /// <param name="path">The JSON Pointer path where the value should be added.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Add(string path, object? value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "add",
                Path = path,
                Value = SerializeValue(value)
            });

            return this;
        }

        /// <summary>
        /// Adds a "remove" operation to the patch document.
        /// </summary>
        /// <param name="path">The JSON Pointer path of the value to remove.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Remove(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "remove",
                Path = path
            });

            return this;
        }

        /// <summary>
        /// Adds a "replace" operation to the patch document.
        /// </summary>
        /// <param name="path">The JSON Pointer path of the value to replace.</param>
        /// <param name="value">The new value.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Replace(string path, object? value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = path,
                Value = SerializeValue(value)
            });

            return this;
        }

        /// <summary>
        /// Adds a "move" operation to the patch document.
        /// </summary>
        /// <param name="from">The JSON Pointer path of the value to move.</param>
        /// <param name="path">The JSON Pointer path where the value should be moved to.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Move(string from, string path)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("From path cannot be null or empty", nameof(from));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "move",
                Path = path,
                From = from
            });

            return this;
        }

        /// <summary>
        /// Adds a "copy" operation to the patch document.
        /// </summary>
        /// <param name="from">The JSON Pointer path of the value to copy.</param>
        /// <param name="path">The JSON Pointer path where the value should be copied to.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Copy(string from, string path)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("From path cannot be null or empty", nameof(from));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "copy",
                Path = path,
                From = from
            });

            return this;
        }

        /// <summary>
        /// Adds a "test" operation to the patch document.
        /// </summary>
        /// <param name="path">The JSON Pointer path of the value to test.</param>
        /// <param name="value">The expected value.</param>
        /// <returns>This JsonPatchDocument instance for method chaining.</returns>
        public JsonPatchDocument Test(string path, object? value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            _operations.Add(new JsonPatchOperation
            {
                Op = "test",
                Path = path,
                Value = SerializeValue(value)
            });

            return this;
        }

        /// <summary>
        /// Applies this patch document to a JSON element.
        /// </summary>
        /// <param name="document">The JSON document to patch.</param>
        /// <returns>The patched JSON element.</returns>
        /// <exception cref="JsonPatchException">Thrown when patch application fails.</exception>
        public JsonElement ApplyTo(JsonElement document)
        {
            if (_operations.Count == 0)
                return document;

            var mutableDocument = CloneJsonElement(document);
            var originalDocument = CloneJsonElement(document);

            try
            {
                for (int i = 0; i < _operations.Count; i++)
                {
                    var operation = _operations[i];
                    mutableDocument = ApplyOperation(mutableDocument, operation, i);
                }

                return mutableDocument;
            }
            catch (JsonPatchException)
            {
                throw; // Re-throw JsonPatchException as-is
            }
            catch (Exception ex)
            {
                throw new JsonPatchException(
                    "Failed to apply JSON patch operations",
                    ex,
                    propertyPath: "/",
                    operation: "patch"
                );
            }
        }

        /// <summary>
        /// Applies this patch document to a .NET object.
        /// </summary>
        /// <typeparam name="T">The type of the object to patch.</typeparam>
        /// <param name="document">The object to patch.</param>
        /// <param name="options">JSON serializer options to use.</param>
        /// <returns>The patched object.</returns>
        /// <exception cref="JsonPatchException">Thrown when patch application fails.</exception>
        public T ApplyTo<T>(T document, JsonSerializerOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            try
            {
                // Serialize to JsonElement
                var json = JsonSerializer.Serialize(document, options);
                var jsonElement = JsonDocument.Parse(json).RootElement;

                // Apply patch
                var patchedElement = ApplyTo(jsonElement);

                // Deserialize back to T
                var patchedJson = JsonSerializer.Serialize(patchedElement, options);
                return JsonSerializer.Deserialize<T>(patchedJson, options)!;
            }
            catch (JsonPatchException)
            {
                throw; // Re-throw JsonPatchException as-is
            }
            catch (Exception ex)
            {
                throw new JsonPatchException(
                    $"Failed to apply JSON patch to object of type {typeof(T).Name}",
                    ex,
                    propertyPath: "/",
                    operation: "patch"
                );
            }
        }

        private static JsonElement SerializeValue(object? value)
        {
            if (value == null)
                return JsonDocument.Parse("null").RootElement;

            if (value is JsonElement element)
                return element;

            var json = JsonSerializer.Serialize(value);
            return JsonDocument.Parse(json).RootElement;
        }

        private static JsonElement CloneJsonElement(JsonElement element)
        {
            var json = JsonSerializer.Serialize(element);
            return JsonDocument.Parse(json).RootElement;
        }

        private JsonElement ApplyOperation(JsonElement document, JsonPatchOperation operation, int operationIndex)
        {
            try
            {
                return operation.Op.ToLowerInvariant() switch
                {
                    "add" => ApplyAdd(document, operation),
                    "remove" => ApplyRemove(document, operation),
                    "replace" => ApplyReplace(document, operation),
                    "move" => ApplyMove(document, operation),
                    "copy" => ApplyCopy(document, operation),
                    "test" => ApplyTest(document, operation),
                    _ => throw new JsonPatchException(
                        $"Unknown operation: {operation.Op}",
                        operation,
                        operationIndex,
                        operation.Path,
                        operation.Op
                    )
                };
            }
            catch (JsonPatchException)
            {
                throw; // Re-throw JsonPatchException as-is
            }
            catch (Exception ex)
            {
                throw new JsonPatchException(
                    $"Failed to apply {operation.Op} operation at path {operation.Path}",
                    ex,
                    operation,
                    operationIndex,
                    operation.Path,
                    operation.Op
                );
            }
        }

        private JsonElement ApplyAdd(JsonElement document, JsonPatchOperation operation)
        {
            var path = ParseJsonPointer(operation.Path);
            return SetValueAtPath(document, path, operation.Value!.Value, createPath: true);
        }

        private JsonElement ApplyRemove(JsonElement document, JsonPatchOperation operation)
        {
            var path = ParseJsonPointer(operation.Path);
            return RemoveValueAtPath(document, path);
        }

        private JsonElement ApplyReplace(JsonElement document, JsonPatchOperation operation)
        {
            var path = ParseJsonPointer(operation.Path);
            return SetValueAtPath(document, path, operation.Value!.Value, createPath: false);
        }

        private JsonElement ApplyMove(JsonElement document, JsonPatchOperation operation)
        {
            var fromPath = ParseJsonPointer(operation.From!);
            var toPath = ParseJsonPointer(operation.Path);

            // Get the value to move
            var valueToMove = GetValueAtPath(document, fromPath);
            
            // Remove from source
            var documentAfterRemove = RemoveValueAtPath(document, fromPath);
            
            // Add to destination
            return SetValueAtPath(documentAfterRemove, toPath, valueToMove, createPath: true);
        }

        private JsonElement ApplyCopy(JsonElement document, JsonPatchOperation operation)
        {
            var fromPath = ParseJsonPointer(operation.From!);
            var toPath = ParseJsonPointer(operation.Path);

            // Get the value to copy
            var valueToCopy = GetValueAtPath(document, fromPath);
            
            // Add to destination
            return SetValueAtPath(document, toPath, valueToCopy, createPath: true);
        }

        private JsonElement ApplyTest(JsonElement document, JsonPatchOperation operation)
        {
            var path = ParseJsonPointer(operation.Path);
            var actualValue = GetValueAtPath(document, path);
            var expectedValue = operation.Value!.Value;

            if (!JsonElementsEqual(actualValue, expectedValue))
            {
                throw new JsonPatchException(
                    $"Test operation failed at path {operation.Path}. Expected: {JsonSerializer.Serialize(expectedValue)}, Actual: {JsonSerializer.Serialize(actualValue)}",
                    operation,
                    propertyPath: operation.Path,
                    operation: "test"
                );
            }

            return document; // Test operations don't modify the document
        }

        private static string[] ParseJsonPointer(string pointer)
        {
            if (pointer == "/")
                return Array.Empty<string>();

            if (!pointer.StartsWith("/"))
                throw new JsonPatchException($"Invalid JSON Pointer: {pointer}. Must start with '/'");

            return pointer.Substring(1)
                .Split('/')
                .Select(UnescapeJsonPointer)
                .ToArray();
        }

        private static string UnescapeJsonPointer(string escaped)
        {
            return escaped.Replace("~1", "/").Replace("~0", "~");
        }

        private static JsonElement GetValueAtPath(JsonElement element, string[] path)
        {
            var current = element;

            foreach (var segment in path)
            {
                if (current.ValueKind == JsonValueKind.Object)
                {
                    if (!current.TryGetProperty(segment, out current))
                        throw new JsonPatchException($"Property '{segment}' not found");
                }
                else if (current.ValueKind == JsonValueKind.Array)
                {
                    if (!int.TryParse(segment, out var index))
                        throw new JsonPatchException($"Invalid array index: {segment}");

                    var array = current.EnumerateArray().ToArray();
                    if (index < 0 || index >= array.Length)
                        throw new JsonPatchException($"Array index {index} out of bounds");

                    current = array[index];
                }
                else
                {
                    throw new JsonPatchException($"Cannot navigate path segment '{segment}' on {current.ValueKind}");
                }
            }

            return current;
        }

        private static JsonElement SetValueAtPath(JsonElement element, string[] path, JsonElement value, bool createPath)
        {
            if (path.Length == 0)
                return value;

            var segment = path[0];
            var remainingPath = path.Skip(1).ToArray();

            if (element.ValueKind == JsonValueKind.Object)
            {
                var properties = new Dictionary<string, JsonElement>();
                
                // Copy existing properties
                foreach (var prop in element.EnumerateObject())
                {
                    properties[prop.Name] = prop.Value;
                }

                // Set the target property
                if (remainingPath.Length == 0)
                {
                    properties[segment] = value;
                }
                else
                {
                    var childElement = properties.TryGetValue(segment, out var existing) 
                        ? existing 
                        : (createPath ? CreateEmptyContainer(remainingPath[0]) : throw new JsonPatchException($"Property '{segment}' not found"));
                    
                    properties[segment] = SetValueAtPath(childElement, remainingPath, value, createPath);
                }

                return CreateJsonObject(properties);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(segment, out var index) && segment != "-")
                    throw new JsonPatchException($"Invalid array index: {segment}");

                var array = element.EnumerateArray().ToArray();
                var newArray = new List<JsonElement>(array);

                if (segment == "-")
                {
                    // Append to end
                    if (remainingPath.Length == 0)
                    {
                        newArray.Add(value);
                    }
                    else
                    {
                        var childElement = createPath ? CreateEmptyContainer(remainingPath[0]) : throw new JsonPatchException("Cannot create path in array append");
                        newArray.Add(SetValueAtPath(childElement, remainingPath, value, createPath));
                    }
                }
                else
                {
                    if (index < 0 || index > array.Length)
                        throw new JsonPatchException($"Array index {index} out of bounds");

                    if (remainingPath.Length == 0)
                    {
                        if (index == array.Length)
                            newArray.Add(value);
                        else
                            newArray[index] = value;
                    }
                    else
                    {
                        var childElement = index < array.Length 
                            ? array[index] 
                            : (createPath ? CreateEmptyContainer(remainingPath[0]) : throw new JsonPatchException($"Array index {index} out of bounds"));
                        
                        if (index == array.Length)
                            newArray.Add(SetValueAtPath(childElement, remainingPath, value, createPath));
                        else
                            newArray[index] = SetValueAtPath(childElement, remainingPath, value, createPath);
                    }
                }

                return CreateJsonArray(newArray);
            }
            else if (createPath)
            {
                // Create new container
                var container = CreateEmptyContainer(segment);
                return SetValueAtPath(container, path, value, createPath);
            }
            else
            {
                throw new JsonPatchException($"Cannot set property '{segment}' on {element.ValueKind}");
            }
        }

        private static JsonElement RemoveValueAtPath(JsonElement element, string[] path)
        {
            if (path.Length == 0)
                throw new JsonPatchException("Cannot remove root element");

            var segment = path[0];
            var remainingPath = path.Skip(1).ToArray();

            if (element.ValueKind == JsonValueKind.Object)
            {
                var properties = new Dictionary<string, JsonElement>();
                
                // Copy existing properties
                foreach (var prop in element.EnumerateObject())
                {
                    properties[prop.Name] = prop.Value;
                }

                if (remainingPath.Length == 0)
                {
                    // Remove this property
                    if (!properties.Remove(segment))
                        throw new JsonPatchException($"Property '{segment}' not found");
                }
                else
                {
                    // Navigate deeper
                    if (!properties.TryGetValue(segment, out var childElement))
                        throw new JsonPatchException($"Property '{segment}' not found");
                    
                    properties[segment] = RemoveValueAtPath(childElement, remainingPath);
                }

                return CreateJsonObject(properties);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(segment, out var index))
                    throw new JsonPatchException($"Invalid array index: {segment}");

                var array = element.EnumerateArray().ToArray();
                
                if (index < 0 || index >= array.Length)
                    throw new JsonPatchException($"Array index {index} out of bounds");

                var newArray = new List<JsonElement>(array);

                if (remainingPath.Length == 0)
                {
                    // Remove this array element
                    newArray.RemoveAt(index);
                }
                else
                {
                    // Navigate deeper
                    newArray[index] = RemoveValueAtPath(array[index], remainingPath);
                }

                return CreateJsonArray(newArray);
            }
            else
            {
                throw new JsonPatchException($"Cannot remove property '{segment}' from {element.ValueKind}");
            }
        }

        private static JsonElement CreateEmptyContainer(string nextSegment)
        {
            // If next segment is numeric or "-", create array, otherwise create object
            if (int.TryParse(nextSegment, out _) || nextSegment == "-")
                return CreateJsonArray(new List<JsonElement>());
            else
                return CreateJsonObject(new Dictionary<string, JsonElement>());
        }

        private static JsonElement CreateJsonObject(Dictionary<string, JsonElement> properties)
        {
            var json = JsonSerializer.Serialize(properties);
            return JsonDocument.Parse(json).RootElement;
        }

        private static JsonElement CreateJsonArray(List<JsonElement> elements)
        {
            var json = JsonSerializer.Serialize(elements);
            return JsonDocument.Parse(json).RootElement;
        }

        private static bool JsonElementsEqual(JsonElement element1, JsonElement element2)
        {
            if (element1.ValueKind != element2.ValueKind)
                return false;

            switch (element1.ValueKind)
            {
                case JsonValueKind.Object:
                    var props1 = new Dictionary<string, JsonElement>();
                    var props2 = new Dictionary<string, JsonElement>();
                    
                    foreach (var prop in element1.EnumerateObject())
                        props1[prop.Name] = prop.Value;
                        
                    foreach (var prop in element2.EnumerateObject())
                        props2[prop.Name] = prop.Value;
                        
                    if (props1.Count != props2.Count)
                        return false;
                        
                    foreach (var kvp in props1)
                    {
                        if (!props2.TryGetValue(kvp.Key, out var value2))
                            return false;
                        if (!JsonElementsEqual(kvp.Value, value2))
                            return false;
                    }
                    return true;

                case JsonValueKind.Array:
                    var array1 = element1.EnumerateArray().ToArray();
                    var array2 = element2.EnumerateArray().ToArray();
                    
                    if (array1.Length != array2.Length)
                        return false;
                        
                    for (int i = 0; i < array1.Length; i++)
                    {
                        if (!JsonElementsEqual(array1[i], array2[i]))
                            return false;
                    }
                    return true;

                case JsonValueKind.String:
                    return element1.GetString() == element2.GetString();
                    
                case JsonValueKind.Number:
                    return Math.Abs(element1.GetDecimal() - element2.GetDecimal()) < 0.0001m;
                    
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element1.GetBoolean() == element2.GetBoolean();
                    
                case JsonValueKind.Null:
                    return true;
                    
                default:
                    return false;
            }
        }
    }
}