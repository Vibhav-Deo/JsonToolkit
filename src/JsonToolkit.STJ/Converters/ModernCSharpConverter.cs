using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Converter factory for modern C# features like records and init-only properties.
    /// </summary>
    public class ModernCSharpConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // Check if type is a record or has init-only properties
            return IsRecordType(typeToConvert) || HasInitOnlyProperties(typeToConvert);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(ModernCSharpConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter?)Activator.CreateInstance(converterType);
        }

        private static bool IsRecordType(Type type)
        {
            // Records have a compiler-generated EqualityContract property
            return type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) != null;
        }

        private static bool HasInitOnlyProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit") == true);
        }
    }

    /// <summary>
    /// Converter for types with modern C# features.
    /// </summary>
    public class ModernCSharpConverter<T> : JsonConverter<T> where T : class
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Try to use primary constructor if available
            var constructors = typeToConvert.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToArray();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0) continue;

                var args = new object?[parameters.Length];
                var allFound = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramName = param.Name ?? "";
                    
                    // Try case-insensitive property lookup
                    JsonElement propElement = default;
                    var found = false;
                    
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, paramName, StringComparison.OrdinalIgnoreCase))
                        {
                            propElement = prop.Value;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        args[i] = JsonSerializer.Deserialize(propElement.GetRawText(), param.ParameterType, options);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        allFound = false;
                        break;
                    }
                }

                if (allFound)
                {
                    var instance = (T?)constructor.Invoke(args);
                    if (instance != null)
                    {
                        // Set any remaining properties not in constructor
                        var constructorParamNames = new HashSet<string>(parameters.Select(p => p.Name ?? ""), StringComparer.OrdinalIgnoreCase);
                        SetRemainingProperties(instance, root, constructorParamNames, options);
                    }
                    return instance;
                }
            }

            // Fallback to default deserialization
            return JsonSerializer.Deserialize<T>(root.GetRawText(), options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static void SetRemainingProperties(T instance, JsonElement root, HashSet<string> constructorParams, JsonSerializerOptions options)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && !constructorParams.Contains(p.Name));

            foreach (var property in properties)
            {
                foreach (var jsonProp in root.EnumerateObject())
                {
                    if (string.Equals(jsonProp.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = JsonSerializer.Deserialize(jsonProp.Value.GetRawText(), property.PropertyType, options);
                        property.SetValue(instance, value);
                        break;
                    }
                }
            }
        }
    }
}
