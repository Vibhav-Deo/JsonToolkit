# JsonToolkit.STJ

[![NuGet Version](https://img.shields.io/nuget/v/JsonToolkit.STJ.svg)](https://www.nuget.org/packages/JsonToolkit.STJ/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JsonToolkit.STJ.svg)](https://www.nuget.org/packages/JsonToolkit.STJ/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**JsonToolkit.STJ** is a comprehensive library that enhances System.Text.Json with developer-friendly features while maintaining high performance. It provides drop-in helpers for common JSON operations, making it easier for developers to migrate from Newtonsoft.Json or work more efficiently with System.Text.Json.

## 🚀 Key Features

- **🔄 Deep Merge**: Recursively merge JSON objects with configurable conflict resolution
- **🩹 JSON Patch**: Apply RFC 6902 JSON Patch operations (add, remove, replace, move, copy, test)
- **🎭 Polymorphic Deserialization**: Simplified handling of inheritance hierarchies with type discriminators
- **⚙️ Optional Property Defaults**: Gracefully handle missing JSON properties with fallback values
- **🔤 Case-Insensitive Matching**: Property matching that works regardless of casing
- **🏷️ Flexible Enums**: String/numeric enum serialization with case-insensitive matching
- **🎯 Enhanced Null Handling**: Distinguish between missing properties, null values, and defaults
- **🔍 JsonPath Queries**: Query JSON documents using JsonPath expressions with wildcards and filters
- **🔗 LINQ-to-JSON**: Familiar LINQ operations for querying and transforming JSON data
- **✅ JSON Schema Validation**: Comprehensive validation with detailed error reporting using JSON Schema
- **🔄 Object Transformation**: Map between different object shapes through JSON with JsonMapper
- **🏷️ Validation Attributes**: Declarative validation with custom attributes (JsonRange, JsonLength, JsonPattern)
- **🆕 Modern C# Support**: Records, init-only properties, required properties, immutable collections
- **🔧 Extension Methods**: Newtonsoft.Json-style convenience methods (ToJson, FromJson, DeepClone)
- **📊 Dynamic Access**: JElement class for JObject-like functionality
- **⚡ Enhanced Error Handling**: Better error messages and context information
- **⚖️ JSON Equality**: Semantic JSON comparison ignoring formatting and property order
- **📋 Configuration Support**: Specialized helpers for appsettings.json scenarios
- **🎯 Multi-Framework**: .NET Framework 4.6.2+, .NET Standard 2.0, .NET 6.0+, .NET 9.0

## 📦 Installation

```bash
dotnet add package JsonToolkit.STJ
```

## 🏃‍♂️ Quick Start

### Basic Usage

```csharp
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;

// Newtonsoft.Json-style extensions
var json = myObject.ToJson();
var obj = json.FromJson<MyType>();
var cloned = myObject.DeepClone();

// System.Text.Json-style enhanced methods
var json2 = JsonSerializer.SerializeEnhanced(myObject, options);
var obj2 = JsonSerializer.DeserializeEnhanced<MyType>(json2, options);
```

### Deep Merge

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

var config1 = """{"database": {"host": "localhost", "port": 5432}}""";
var config2 = """{"database": {"port": 3306, "ssl": true}}""";

var merged = JsonMerge.DeepMerge(
    JsonDocument.Parse(config1).RootElement,
    JsonDocument.Parse(config2).RootElement
);
// Result: {"database": {"host": "localhost", "port": 3306, "ssl": true}}
```

### JSON Patch

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

var document = JsonDocument.Parse("""{"name": "John", "age": 30}""");

var patch = new JsonPatchDocument()
    .Replace("/age", 31)
    .Add("/email", "john@example.com")
    .Test("/name", "John");

var result = patch.ApplyTo(document.RootElement);
// Result: {"name": "John", "age": 31, "email": "john@example.com"}
```

### JsonPath Queries

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

var json = """
{
  "users": [
    {"name": "John", "age": 30},
    {"name": "Jane", "age": 25}
  ]
}
""";

var adults = JsonPath.Query(JsonDocument.Parse(json).RootElement, "$.users[?(@.age >= 18)]");
// Returns both users since both are 18 or older
```

### JSON Schema Validation

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

var schema = """
{
  "type": "object",
  "properties": {
    "name": {"type": "string"},
    "age": {"type": "number", "minimum": 0}
  },
  "required": ["name", "age"]
}
""";

var validator = new JsonSchemaValidator(schema);
var json = """{"name": "John", "age": 30}""";

var result = validator.Validate(json);
if (result.IsValid)
{
    Console.WriteLine("JSON is valid!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at {error.Path}: {error.Message}");
    }
}
```

### Object Mapping

```csharp
using JsonToolkit.STJ;

// Define source and target types
public class UserDto
{
    public string FullName { get; set; }
    public int YearsOld { get; set; }
}

public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string DisplayName { get; set; }
}

// Configure mapping
var mapper = JsonMapper.Create()
    .Map<UserDto, User>(config => config
        .ForMember(u => u.Name, dto => dto.FullName)
        .ForMember(u => u.Age, dto => dto.YearsOld)
        .ForMember(u => u.DisplayName, dto => $"User: {dto.FullName}"));

// Transform objects
var dto = new UserDto { FullName = "John Doe", YearsOld = 30 };
var user = mapper.Transform<UserDto, User>(dto);
// Result: User with Name="John Doe", Age=30, DisplayName="User: John Doe"
```

### Configuration with Fluent API

```csharp
using JsonToolkit.STJ;

var options = new JsonOptionsBuilder()
    .WithCaseInsensitiveProperties()
    .WithFlexibleEnums()
    .WithOptionalDefaults(new { timeout = 30, retries = 3 })
    .WithPolymorphicTypes(config => config
        .ForBaseType<Animal>()
        .WithDiscriminator("type")
        .MapType("dog", typeof(Dog))
        .MapType("cat", typeof(Cat)))
    .Build();

// Use the configured options
var json = JsonSerializer.Serialize(myObject, options);
var obj = JsonSerializer.Deserialize<MyType>(json, options);
```

## 🌟 Real-World Examples

### Configuration File Merging

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

// Base configuration
var baseConfig = """
{
  "database": {
    "host": "localhost",
    "port": 5432,
    "timeout": 30
  },
  "logging": {
    "level": "Information"
  }
}
""";

// Environment-specific overrides
var prodConfig = """
{
  "database": {
    "host": "prod-db.company.com",
    "ssl": true
  },
  "logging": {
    "level": "Warning"
  }
}
""";

// Merge configurations
var merged = JsonMerge.DeepMerge(
    JsonDocument.Parse(baseConfig).RootElement,
    JsonDocument.Parse(prodConfig).RootElement
);

// Result: Combined configuration with production overrides
// {
//   "database": {
//     "host": "prod-db.company.com",
//     "port": 5432,
//     "timeout": 30,
//     "ssl": true
//   },
//   "logging": {
//     "level": "Warning"
//   }
// }
```

### REST API Integration

```csharp
using Microsoft.AspNetCore.Mvc;
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate the request using validation attributes
            var validationResult = request.Validate();
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Transform DTO to domain model
            var mapper = JsonMapper.Create();
            var user = mapper.Transform<CreateUserRequest, User>(request);

            // Save user (implementation omitted)
            await SaveUserAsync(user);

            // Return response
            var response = new { Id = user.Id, Message = "User created successfully" };
            return Ok(response.ToJson());
        }
        catch (JsonValidationException ex)
        {
            return BadRequest(new { Error = "Validation failed", Details = ex.Errors });
        }
    }
}

public class CreateUserRequest
{
    [JsonLength(2, 50)]
    public string Name { get; set; }

    [JsonPattern(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    public string Email { get; set; }

    [JsonRange(18, 120)]
    public int Age { get; set; }
}
```

### Complex Data Transformation

```csharp
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;

// Transform complex nested data structures
var sourceData = new
{
    customer = new
    {
        personal_info = new { first_name = "John", last_name = "Doe" },
        contact = new { email = "john@example.com", phone = "555-1234" }
    },
    orders = new[]
    {
        new { id = 1, amount = 99.99, status = "completed" },
        new { id = 2, amount = 149.50, status = "pending" }
    }
};

// Configure transformation mapping
var mapper = JsonMapper.Create()
    .Map<dynamic, CustomerSummary>(config => config
        .ForMember(c => c.FullName, src => $"{src.customer.personal_info.first_name} {src.customer.personal_info.last_name}")
        .ForMember(c => c.Email, src => src.customer.contact.email)
        .ForMember(c => c.TotalOrders, src => src.orders.Length)
        .ForMember(c => c.TotalAmount, src => src.orders.Sum(o => o.amount)));

var summary = mapper.Transform<dynamic, CustomerSummary>(sourceData);
// Result: CustomerSummary with aggregated data
```

## 📚 Documentation

### Migration from Newtonsoft.Json

JsonToolkit.STJ provides familiar APIs to ease migration:

```csharp
using JsonToolkit.STJ.Extensions;

// Before (Newtonsoft.Json)
var json = JsonConvert.SerializeObject(obj);
var obj = JsonConvert.DeserializeObject<MyType>(json);
var jobj = JObject.Parse(json);
var value = jobj["property"]["nested"].Value<string>();

// After (JsonToolkit.STJ)
var json = obj.ToJson();
var obj = json.FromJson<MyType>();
var jelem = JElement.Parse(json);
var value = jelem["property"]["nested"].Value<string>();
```

### Advanced Features

#### Polymorphic Deserialization

```csharp
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;

public abstract class Shape
{
    public string Color { get; set; }
}

public class Circle : Shape
{
    public double Radius { get; set; }
}

public class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }
}

var options = new JsonOptionsBuilder()
    .WithPolymorphicTypes(config => config
        .ForBaseType<Shape>()
        .WithDiscriminator("$type")
        .MapType("circle", typeof(Circle))
        .MapType("rectangle", typeof(Rectangle)))
    .Build();

var json = """{"$type": "circle", "color": "red", "radius": 5.0}""";
var shape = json.FromJson<Shape>(options); // Returns Circle instance
```

#### Validation Attributes

```csharp
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;
using JsonToolkit.STJ.ValidationAttributes;

public class User
{
    [JsonRange(1, 120)]
    public int Age { get; set; }
    
    [JsonLength(2, 50)]
    public string Name { get; set; }
    
    [JsonPattern(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    public string Email { get; set; }
}

// Validate during deserialization
var json = """{"name": "John", "age": 30, "email": "john@example.com"}""";
var user = json.ValidateAndDeserialize<User>(); // Throws JsonValidationException if invalid

// Or validate an existing object
var existingUser = new User { Name = "John", Age = 30, Email = "john@example.com" };
var validationResult = existingUser.Validate();
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
```

## 🔧 Troubleshooting & Common Issues

### JSON Patch Path Casing

**Problem**: JSON Patch operations fail with "path not found" errors.

**Solution**: JSON Patch paths are case-sensitive and must match the exact property names in your JSON:

```csharp
// ❌ Wrong - property name casing doesn't match
var patch = new JsonPatchDocument()
    .Replace("/name", "Jane");  // JSON has "Name" not "name"

// ✅ Correct - exact case match
var patch = new JsonPatchDocument()
    .Replace("/Name", "Jane");  // Matches JSON property exactly
```

### Validation Not Triggering

**Problem**: Validation attributes are ignored during deserialization.

**Solution**: Ensure validation is enabled in your JsonSerializerOptions:

```csharp
// ❌ Wrong - validation not enabled
var options = new JsonSerializerOptions();
var user = JsonSerializer.Deserialize<User>(json, options);

// ✅ Correct - validation enabled
var options = new JsonSerializerOptions().WithValidation();
var user = JsonSerializer.Deserialize<User>(json, options);

// Or use the extension method
var user = json.ValidateAndDeserialize<User>();
```

### Performance Optimization

**Problem**: Slow JSON processing in high-throughput scenarios.

**Solution**: Reuse JsonSerializerOptions and avoid repeated configuration:

```csharp
// ❌ Wrong - creates new options every time
public string SerializeUser(User user)
{
    var options = new JsonOptionsBuilder().WithCaseInsensitiveProperties().Build();
    return JsonSerializer.Serialize(user, options);
}

// ✅ Correct - reuse configured options
private static readonly JsonSerializerOptions _options = 
    new JsonOptionsBuilder().WithCaseInsensitiveProperties().Build();

public string SerializeUser(User user)
{
    return JsonSerializer.Serialize(user, _options);
}
```

### Migration from Newtonsoft.Json Gotchas

**Key Differences to Watch:**

1. **Property Naming**: System.Text.Json uses PascalCase by default, Newtonsoft.Json uses the original property names
2. **Null Handling**: Different default behaviors for null values
3. **Date Formats**: Different default date serialization formats

```csharp
// Configure JsonToolkit.STJ to match Newtonsoft.Json behavior
var options = new JsonOptionsBuilder()
    .WithCaseInsensitiveProperties()  // Handle casing differences
    .WithFlexibleEnums()              // Handle enum string/number flexibility
    .Build();
```

## 📖 Additional Features

### JSON Equality Comparison

```csharp
using JsonToolkit.STJ.Extensions;

var json1 = """{"name": "John", "age": 30}""";
var json2 = """{"age": 30, "name": "John"}""";  // Different property order

// Semantic equality (ignores property order)
bool areEqual = json1.SemanticEquals(json2);  // Returns true

// For arrays, you can choose whether order matters
var array1 = """{"items": [1, 2, 3]}""";
var array2 = """{"items": [3, 2, 1]}""";

bool orderSensitive = array1.SemanticEquals(array2, orderSensitive: true);   // false
bool orderInsensitive = array1.SemanticEquals(array2, orderSensitive: false); // true
```

### Async Stream Operations

```csharp
using JsonToolkit.STJ;
using JsonToolkit.STJ.Extensions;

// Async serialization to stream
var user = new User { Name = "John", Age = 30 };
using var stream = new FileStream("user.json", FileMode.Create);
await user.ToJsonAsync(stream);

// Async deserialization from stream
using var readStream = new FileStream("user.json", FileMode.Open);
var deserializedUser = await JsonSerializer.DeserializeEnhancedAsync<User>(readStream);

// Enhanced async methods with better error handling
await JsonSerializer.SerializeEnhancedAsync(stream, user, options);
var result = await JsonSerializer.DeserializeEnhancedAsync<User>(stream, options);
```

### Byte Array Operations

```csharp
using JsonToolkit.STJ.Extensions;

var user = new User { Name = "John", Age = 30 };

// Serialize to UTF-8 bytes
byte[] jsonBytes = user.ToJsonBytes();

// Enhanced serialization with error handling
byte[] enhancedBytes = JsonSerializer.SerializeEnhancedToUtf8Bytes(user, options);

// Deserialize from bytes
var deserializedUser = JsonSerializer.DeserializeEnhanced<User>(jsonBytes, options);
```

### LINQ-to-JSON Operations

```csharp
using System.Text.Json;
using JsonToolkit.STJ;

var json = """
{
  "products": [
    {"name": "Laptop", "price": 999.99, "category": "Electronics"},
    {"name": "Book", "price": 19.99, "category": "Education"},
    {"name": "Phone", "price": 599.99, "category": "Electronics"}
  ]
}
""";

var document = JsonDocument.Parse(json);
var products = document.RootElement.GetProperty("products");

// Filter products using LINQ-style methods
var expensiveProducts = products.Where(p => 
    p.GetProperty("price").GetDouble() > 100);

// Project to new values
var productNames = products.Select(p => 
    p.GetProperty("name").GetString());

// Aggregate operations
var totalValue = products.Sum(p => 
    p.GetProperty("price").GetDouble());

var averagePrice = products.Average(p => 
    p.GetProperty("price").GetDouble());

// Find specific items
var firstElectronics = products.FirstOrDefault(p => 
    p.GetProperty("category").GetString() == "Electronics");

// Count items
var electronicsCount = products.Count(p => 
    p.GetProperty("category").GetString() == "Electronics");
```

## 🎯 Performance

JsonToolkit.STJ maintains System.Text.Json's performance characteristics while adding powerful features. Here are benchmark results from our comprehensive performance tests:

### Benchmark Results

| Operation | .NET 9.0 | .NET 8.0 | .NET Framework 4.6.2 | Description |
|-----------|----------|----------|----------------------|-------------|
| **Basic Serialization** | 100ms (1000 iterations) | 53ms (1000 iterations) | 385ms (1000 iterations) | Raw serialization performance vs System.Text.Json |
| **Basic Deserialization** | 200ms (1000 iterations) | 111ms (1000 iterations) | 768ms (1000 iterations) | Raw deserialization performance vs System.Text.Json |
| **Deep Merge** | 0.03ms per operation | 0.03ms per operation | 0.08ms per operation | Recursive JSON object merging |
| **JSON Patch** | 0.02ms per operation | 0.02ms per operation | 0.09ms per operation | RFC 6902 patch operations |
| **JsonPath Query** | 0.64ms per operation | 0.66ms per operation | 2.70ms per operation | Query 1000 items with filter |
| **JElement Access** | 0.001ms per operation | 0.002ms per operation | 0.006ms per operation | Dynamic property access |
| **Large Document (1.2MB)** | Serialize: 6ms, Deserialize: 13ms | Serialize: 2ms, Deserialize: 12ms | Serialize: 23ms, Deserialize: 41ms | Processing large JSON documents |
| **Memory Usage** | ~4.3MB increase (100 operations) | ~2.5MB increase (100 operations) | ~3.3MB increase (100 operations) | Memory overhead for mixed operations |

### Performance Characteristics

- **Memory Efficient**: Minimal allocations through efficient algorithms and object pooling
- **High Throughput**: Performance identical to raw System.Text.Json for basic operations
- **Streaming Support**: Async-friendly APIs for large documents
- **Framework Optimized**: Better performance on modern .NET compared to .NET Framework
- **Scalable**: Efficient processing of large documents (1MB+ tested)

## 🔧 Framework Support

| Framework | Version | Status |
|-----------|---------|--------|
| .NET Framework | 4.6.2+ | ✅ Supported |
| .NET Standard | 2.0+ | ✅ Supported |
| .NET Core | 3.1+ | ✅ Supported |
| .NET | 5.0+ | ✅ Supported |

## 🚀 CI/CD Pipeline

This project uses GitHub Actions for continuous integration and automated NuGet publishing with modern security practices.

### Build Status

[![CI](https://github.com/your-username/JsonToolkit.STJ/actions/workflows/ci.yml/badge.svg)](https://github.com/your-username/JsonToolkit.STJ/actions/workflows/ci.yml)
[![Release](https://github.com/your-username/JsonToolkit.STJ/actions/workflows/release.yml/badge.svg)](https://github.com/your-username/JsonToolkit.STJ/actions/workflows/release.yml)

### Features

- **🔒 Secure Publishing**: Uses NuGet Trusted Publishing with OIDC (no API keys stored)
- **🧪 Comprehensive Testing**: Unit tests and property-based tests across multiple frameworks
- **📊 Coverage Reports**: Automated code coverage reporting and artifact collection
- **🏗️ Multi-Framework Builds**: Tests on .NET Framework 4.6.2, .NET 6.0, .NET 8.0, and .NET 9.0
- **⚡ Optimized Performance**: Efficient caching and parallel execution
- **📦 Automated Releases**: Version tags trigger automatic NuGet publishing

### Release Process

1. **Create Version Tag**: Push a tag in format `v{MAJOR}.{MINOR}.{PATCH}` (e.g., `v1.2.3`)
2. **Automated Build**: GitHub Actions builds and tests across all target frameworks
3. **Package Creation**: NuGet package created with proper versioning and metadata
4. **Secure Publishing**: Package published to NuGet.org using OIDC authentication
5. **GitHub Release**: Automated GitHub release created with artifacts and checksums

### Development Workflow

```bash
# Run tests locally
dotnet test

# Build release package
dotnet pack --configuration Release

# Validate OIDC setup (in GitHub Actions)
./scripts/validate-oidc-setup.sh
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built on top of Microsoft's excellent [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)
- Inspired by the convenience of [Newtonsoft.Json](https://www.newtonsoft.com/json)
- Property-based testing with [FsCheck](https://fscheck.github.io/FsCheck/)

---

**JsonToolkit.STJ** - Bridging the gap between System.Text.Json performance and Newtonsoft.Json convenience.