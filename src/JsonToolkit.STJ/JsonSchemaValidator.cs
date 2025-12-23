using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonToolkit.STJ;

public class JsonSchemaValidator
{
    private readonly JsonElement _schema;

    public JsonSchemaValidator(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new ArgumentNullException(nameof(schemaJson));

        using var doc = JsonDocument.Parse(schemaJson);
        _schema = doc.RootElement.Clone();
    }

    public JsonSchemaValidator(JsonElement schema)
    {
        _schema = schema.Clone();
    }

    public ValidationResult Validate(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ValidationResult.Failure(new ValidationError("$", "JSON cannot be null or empty", "NullJson"));

        using var doc = JsonDocument.Parse(json);
        return Validate(doc.RootElement);
    }

    public ValidationResult Validate(JsonElement element)
    {
        var errors = new List<ValidationError>();
        ValidateElement(element, _schema, "$", errors);
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    private void ValidateElement(JsonElement element, JsonElement schema, string path, List<ValidationError> errors)
    {
        if (schema.TryGetProperty("type", out var typeProperty))
        {
            var expectedType = typeProperty.GetString();
            if (!ValidateType(element, expectedType))
            {
                errors.Add(new ValidationError(path, $"Expected type '{expectedType}' but got '{GetJsonType(element)}'", "TypeError"));
                return;
            }
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(element, schema, path, errors);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            ValidateArray(element, schema, path, errors);
        }
    }

    private void ValidateObject(JsonElement element, JsonElement schema, string path, List<ValidationError> errors)
    {
        if (schema.TryGetProperty("required", out var requiredProperty) && requiredProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var reqProp in requiredProperty.EnumerateArray())
            {
                var propName = reqProp.GetString();
                if (propName != null && !element.TryGetProperty(propName, out _))
                {
                    errors.Add(new ValidationError($"{path}.{propName}", $"Required property '{propName}' is missing", "RequiredProperty"));
                }
            }
        }

        if (schema.TryGetProperty("properties", out var propertiesSchema))
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (propertiesSchema.TryGetProperty(prop.Name, out var propSchema))
                {
                    ValidateElement(prop.Value, propSchema, $"{path}.{prop.Name}", errors);
                }
            }
        }
    }

    private void ValidateArray(JsonElement element, JsonElement schema, string path, List<ValidationError> errors)
    {
        if (schema.TryGetProperty("items", out var itemsSchema))
        {
            int index = 0;
            foreach (var item in element.EnumerateArray())
            {
                ValidateElement(item, itemsSchema, $"{path}[{index}]", errors);
                index++;
            }
        }
    }

    private bool ValidateType(JsonElement element, string expectedType)
    {
        return expectedType?.ToLowerInvariant() switch
        {
            "object" => element.ValueKind == JsonValueKind.Object,
            "array" => element.ValueKind == JsonValueKind.Array,
            "string" => element.ValueKind == JsonValueKind.String,
            "number" => element.ValueKind == JsonValueKind.Number,
            "integer" => element.ValueKind == JsonValueKind.Number && IsInteger(element),
            "boolean" => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False,
            "null" => element.ValueKind == JsonValueKind.Null,
            _ => true
        };
    }

    private bool IsInteger(JsonElement element)
    {
        if (element.TryGetInt64(out _)) return true;
        if (element.TryGetDouble(out var d)) return d == Math.Floor(d);
        return false;
    }

    private string GetJsonType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True => "boolean",
            JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "undefined"
        };
    }
}
