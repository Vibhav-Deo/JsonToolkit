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
- **✅ JSON Schema Validation**: Comprehensive validation with detailed error reporting
- **📋 Configuration Support**: Specialized helpers for appsettings.json scenarios
- **🔄 Object Transformation**: Map between different object shapes through JSON
- **🏷️ Validation Attributes**: Declarative validation with custom attributes
- **🆕 Modern C# Support**: Records, init-only properties, required properties, immutable collections
- **🔧 Extension Methods**: Newtonsoft.Json-style convenience methods (ToJson, FromJson, DeepClone)
- **📊 Dynamic Access**: JElement class for JObject-like functionality
- **🎯 Multi-Framework**: .NET Framework 4.6.2+, .NET Standard 2.0, .NET 6.0+, .NET 9.0

## 📦 Installation

```bash
dotnet add package JsonToolkit.STJ
```

## 🏃‍♂️ Quick Start

### Basic Usage

```csharp
using JsonToolkit.STJ;

// Newtonsoft.Json-style extensions
var json = myObject.ToJson();
var obj = json.FromJson<MyType>();
var cloned = myObject.DeepClone();

// System.Text.Json-style enhanced methods
var json = JsonSerializer.SerializeEnhanced(myObject, options);
var obj = JsonSerializer.DeserializeEnhanced<MyType>(json, options);
```

### Deep Merge

```csharp
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
var document = JsonDocument.Parse("""{"name": "John", "age": 30}""");

var patch = new JsonPatchDocument()
    .Replace("/age", 31)
    .Add("/email", "john@example.com")
    .Test("/name", "John");

var result = patch.ApplyTo(document.RootElement);
```

### JsonPath Queries

```csharp
var json = """
{
  "users": [
    {"name": "John", "age": 30},
    {"name": "Jane", "age": 25}
  ]
}
""";

var adults = JsonPath.Query(JsonDocument.Parse(json).RootElement, "$.users[?(@.age >= 18)]");
```

### Configuration with Fluent API

```csharp
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
```

## 📚 Documentation

### Migration from Newtonsoft.Json

JsonToolkit.STJ provides familiar APIs to ease migration:

```csharp
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
public class User
{
    [JsonRange(1, 120)]
    public int Age { get; set; }
    
    [JsonLength(2, 50)]
    public string Name { get; set; }
    
    [JsonPattern(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    public string Email { get; set; }
}

var user = json.ValidateAndDeserialize<User>(); // Throws with validation details
```

## 🎯 Performance

JsonToolkit.STJ maintains System.Text.Json's performance characteristics while adding powerful features. Here are benchmark results from our comprehensive performance tests:

### Benchmark Results

| Operation | .NET 9.0 | .NET 8.0 | .NET Framework 4.6.2 | Description |
|-----------|----------|----------|----------------------|-------------|
| **Basic Serialization** | TBD | 53ms (1000 iterations) | 389ms (1000 iterations) | Raw serialization performance vs System.Text.Json |
| **Basic Deserialization** | TBD | 143ms (1000 iterations) | 779ms (1000 iterations) | Raw deserialization performance vs System.Text.Json |
| **Deep Merge** | TBD | 0.03ms per operation | 0.09ms per operation | Recursive JSON object merging |
| **JSON Patch** | TBD | 0.02ms per operation | 0.14ms per operation | RFC 6902 patch operations |
| **JsonPath Query** | TBD | 0.70ms per operation | 2.70ms per operation | Query 1000 items with filter |
| **JElement Access** | TBD | 0.002ms per operation | 0.007ms per operation | Dynamic property access |
| **Large Document (1.2MB)** | TBD | Serialize: 3ms, Deserialize: 12ms | Serialize: 22ms, Deserialize: 45ms | Processing large JSON documents |
| **Memory Usage** | TBD | ~3MB increase (100 operations) | ~3.3MB increase (100 operations) | Memory overhead for mixed operations |

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

### NuGet Trusted Publishing

This repository uses **NuGet Trusted Publishing** with OpenID Connect (OIDC) for secure, keyless package publishing:

- **No API Keys**: No sensitive credentials stored in repository secrets
- **Enhanced Security**: Short-lived tokens generated automatically by GitHub
- **Audit Trail**: Complete publishing history with GitHub Actions logs
- **Zero Maintenance**: No key rotation or secret management required

For setup instructions, see [NuGet Trusted Publishing Guide](docs/NUGET_TRUSTED_PUBLISHING.md).

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