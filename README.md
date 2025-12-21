# JsonToolkit.STJ

Drop-in helpers to address common pain points when working with System.Text.Json in .NET applications. Provides Newtonsoft.Json-like conveniences while maintaining System.Text.Json performance benefits.

## Features

- **Deep Merge**: Recursively merge JSON objects with configurable conflict resolution
- **JSON Patch**: Apply RFC 6902 JSON Patch operations to documents
- **Enhanced Converters**: Simplified custom converter creation with base classes
- **Polymorphic Deserialization**: Easy handling of inheritance hierarchies
- **Optional Property Defaults**: Graceful handling of missing JSON properties
- **Case-Insensitive Matching**: Default case-insensitive property matching
- **Modern C# Support**: Full support for records, init-only properties, and immutable collections
- **JsonPath Queries**: Query JSON documents using JsonPath expressions
- **LINQ-to-JSON**: LINQ-style operations over JSON documents
- **Multi-Framework Support**: Works on .NET Framework 4.6.1+, .NET Standard 2.0, .NET 6.0, and .NET 8.0

## Installation

```bash
dotnet add package JsonToolkit.STJ
```

## Quick Start

```csharp
using JsonToolkit.STJ;

// Newtonsoft.Json-style extensions
var json = myObject.ToJson();
var obj = json.FromJson<MyType>();

// Enhanced System.Text.Json methods
var merged = JsonSerializer.DeepMerge(obj1, obj2);
var patched = JsonSerializer.ApplyPatch(document, patchDoc);

// JObject-like functionality
var element = JElement.Parse(json);
var value = element["property"]["nested"].Value<string>();
```

## Target Frameworks

- .NET Framework 4.6.1
- .NET Standard 2.0
- .NET 6.0
- .NET 8.0

## License

MIT License - see LICENSE file for details.