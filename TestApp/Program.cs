using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonToolkit.STJ;

public class TestPerson
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string? Email { get; set; }
}

public enum TestEnum
{
    Active,
    Inactive,
    Pending
}

class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("=== Test: Converter Detection ===");
            TestConverterDetection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }
    
    static void TestConverterDetection()
    {
        var builder = new JsonOptionsBuilder();
        builder.WithFlexibleEnums();
        var options = builder.Build();

        Console.WriteLine($"Converters count: {options.Converters.Count}");
        foreach (var converter in options.Converters)
        {
            var typeName = converter.GetType().Name;
            Console.WriteLine($"Converter: {typeName}");
            Console.WriteLine($"  Is JsonStringEnumConverter: {converter is JsonStringEnumConverter}");
            Console.WriteLine($"  Contains 'FlexibleEnum': {typeName.Contains("FlexibleEnum")}");
            Console.WriteLine($"  Is JsonConverterFactory: {converter is JsonConverterFactory}");
            if (converter is JsonConverterFactory factory)
            {
                Console.WriteLine($"  Factory type name: {factory.GetType().Name}");
                Console.WriteLine($"  Factory contains 'FlexibleEnum': {factory.GetType().Name.Contains("FlexibleEnum")}");
            }
        }

        // Test the verification logic
        bool hasEnumConverter = false;
        foreach (var converter in options.Converters)
        {
            var converterTypeName = converter.GetType().Name;
            if (converter is JsonStringEnumConverter || 
                converterTypeName.Contains("FlexibleEnum") ||
                converter is JsonConverterFactory factory && factory.GetType().Name.Contains("FlexibleEnum"))
            {
                hasEnumConverter = true;
                break;
            }
        }
        Console.WriteLine($"\nHas enum converter: {hasEnumConverter}");
        
        // Test serialization
        var testObject = new { Name = "Test", Value = 42, Status = TestEnum.Active };
        var json = JsonSerializer.Serialize(testObject, options);
        Console.WriteLine($"\nSerialized JSON: {json}");
    }
}