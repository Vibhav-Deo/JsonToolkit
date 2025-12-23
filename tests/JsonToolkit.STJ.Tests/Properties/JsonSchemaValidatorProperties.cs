using FsCheck;
using FsCheck.Xunit;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties;

public class JsonSchemaValidatorProperties
{
    [Property]
    public Property SchemaValidation_ValidObjectShouldPass()
    {
        return Prop.ForAll<string, int>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            Arb.From<int>(),
            (name, value) =>
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""number"" }
                    }
                }";

                var json = JsonSerializer.Serialize(new { Name = name, Value = value });
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return result.IsValid.Label("Valid object should pass validation");
            });
    }

    [Property]
    public Property SchemaValidation_InvalidTypeShouldFail()
    {
        return Prop.ForAll<int>(
            Arb.From<int>(),
            value =>
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Value"": { ""type"": ""string"" }
                    }
                }";

                var json = JsonSerializer.Serialize(new { Value = value });
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return (!result.IsValid && result.Errors.Any(e => e.ErrorType == "TypeError"))
                    .Label("Invalid type should fail validation");
            });
    }

    [Property]
    public Property SchemaValidation_MissingRequiredPropertyShouldFail()
    {
        return Prop.ForAll<int>(
            Arb.From<int>(),
            value =>
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""required"": [""Name"", ""Value""],
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""number"" }
                    }
                }";

                var json = JsonSerializer.Serialize(new { Value = value });
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return (!result.IsValid && result.Errors.Any(e => e.ErrorType == "RequiredProperty"))
                    .Label("Missing required property should fail validation");
            });
    }

    [Property]
    public Property SchemaValidation_NestedObjectsShouldValidate()
    {
        return Prop.ForAll<string, int>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            Arb.From<int>(),
            (name, value) =>
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Nested"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Name"": { ""type"": ""string"" },
                                ""Value"": { ""type"": ""number"" }
                            }
                        }
                    }
                }";

                var json = JsonSerializer.Serialize(new { Nested = new { Name = name, Value = value } });
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return result.IsValid.Label("Nested objects should validate correctly");
            });
    }

    [Property]
    public Property SchemaValidation_ArrayItemsShouldValidate()
    {
        return Prop.ForAll<int[]>(
            Arb.Default.Array<int>().Generator.Where(arr => arr != null && arr.Length > 0 && arr.Length < 10).ToArbitrary(),
            arr =>
            {
                var schema = @"{
                    ""type"": ""array"",
                    ""items"": { ""type"": ""number"" }
                }";

                var json = JsonSerializer.Serialize(arr);
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return result.IsValid.Label("Array items should validate correctly");
            });
    }

    [Property]
    public Property SchemaValidation_ErrorsShouldContainPropertyPath()
    {
        return Prop.ForAll<string>(
            Arb.Default.NonEmptyString().Generator.Where(s => s != null && s.Get.Length < 50).Select(s => s.Get).ToArbitrary(),
            name =>
            {
                var schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Value"": { ""type"": ""number"" }
                    }
                }";

                var json = JsonSerializer.Serialize(new { Value = name });
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return (!result.IsValid && result.Errors.All(e => !string.IsNullOrEmpty(e.PropertyPath)))
                    .Label("Errors should contain property path");
            });
    }

    [Property]
    public Property SchemaValidation_EmptyJsonShouldFail()
    {
        return Prop.ForAll(
            Gen.Constant(1).ToArbitrary(),
            _ =>
            {
                var schema = @"{ ""type"": ""object"" }";
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate("");

                return (!result.IsValid).Label("Empty JSON should fail validation");
            });
    }

    [Property]
    public Property SchemaValidation_ValidArrayShouldPass()
    {
        return Prop.ForAll<string[]>(
            Arb.Default.Array<string>().Generator.Where(arr => arr != null && arr.All(s => s != null) && arr.Length < 10).ToArbitrary(),
            arr =>
            {
                var schema = @"{
                    ""type"": ""array"",
                    ""items"": { ""type"": ""string"" }
                }";

                var json = JsonSerializer.Serialize(arr);
                var validator = new JsonSchemaValidator(schema);
                var result = validator.Validate(json);

                return result.IsValid.Label("Valid array should pass validation");
            });
    }
}
