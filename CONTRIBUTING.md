# Contributing to JsonToolkit.STJ

Thank you for your interest in contributing to JsonToolkit.STJ! We welcome contributions from the community and are grateful for your help in making this library better.

## 🚀 Getting Started

### Prerequisites

- .NET SDK 6.0 or later (for development)
- Git
- A GitHub account

### Development Setup

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/your-username/JsonToolkit.STJ.git
   cd JsonToolkit.STJ
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run tests** to ensure everything works:
   ```bash
   dotnet test
   ```

## 🛠️ Development Workflow

### Making Changes

1. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following our coding standards
3. **Add tests** for new functionality
4. **Run tests** to ensure nothing is broken:
   ```bash
   dotnet test
   ```

5. **Commit your changes** with a clear message:
   ```bash
   git commit -m "Add feature: description of your changes"
   ```

6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request** on GitHub

### Pull Request Guidelines

- **Clear Title**: Use a descriptive title that explains what the PR does
- **Description**: Provide a detailed description of your changes
- **Link Issues**: Reference any related issues using `Fixes #123` or `Closes #123`
- **Tests**: Ensure all tests pass and add new tests for new functionality
- **Documentation**: Update documentation if your changes affect the public API

## 🧪 Testing

We use a comprehensive testing approach with both unit tests and property-based tests:

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific framework
dotnet test --framework net8.0

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run only unit tests
dotnet test --filter "Category!=Property"

# Run only property-based tests
dotnet test --filter "Category=Property"
```

### Writing Tests

- **Unit Tests**: Place in `tests/JsonToolkit.STJ.Tests/Unit/`
- **Property Tests**: Place in `tests/JsonToolkit.STJ.Tests/Properties/`
- **Integration Tests**: Place in `tests/JsonToolkit.STJ.Tests/Integration/`

#### Unit Test Example

```csharp
[Test]
public void DeepMerge_ShouldMergeSimpleObjects()
{
    // Arrange
    var source = JsonDocument.Parse("""{"a": 1}""").RootElement;
    var target = JsonDocument.Parse("""{"b": 2}""").RootElement;

    // Act
    var result = JsonMerge.DeepMerge(source, target);

    // Assert
    result.GetProperty("a").GetInt32().Should().Be(1);
    result.GetProperty("b").GetInt32().Should().Be(2);
}
```

#### Property Test Example

```csharp
[Property]
[Category("Property")]
public Property RoundTripSerialization_PreservesData(NonNull<TestObject> obj)
{
    return Prop.ForAll<TestObject>(original =>
    {
        var json = original.ToJson();
        var deserialized = json.FromJson<TestObject>();
        return original.Equals(deserialized);
    });
}
```

## 📝 Coding Standards

### General Guidelines

- **Follow .NET conventions**: Use PascalCase for public members, camelCase for private fields
- **XML Documentation**: Add XML docs for all public APIs
- **Null Safety**: Use nullable reference types appropriately
- **Performance**: Consider performance implications, especially for hot paths
- **Compatibility**: Maintain backward compatibility when possible

### Code Style

We follow standard .NET coding conventions:

```csharp
// Good
public class JsonMerge
{
    private readonly JsonSerializerOptions _options;
    
    /// <summary>
    /// Merges two JSON elements recursively.
    /// </summary>
    /// <param name="source">The source JSON element.</param>
    /// <param name="target">The target JSON element to merge into.</param>
    /// <returns>The merged JSON element.</returns>
    public static JsonElement DeepMerge(JsonElement source, JsonElement target)
    {
        // Implementation
    }
}
```

### Performance Considerations

- Use `Span<T>` and `Memory<T>` for high-performance scenarios
- Avoid unnecessary allocations in hot paths
- Consider using object pooling for frequently created objects
- Profile performance-critical changes

## 🐛 Reporting Issues

### Bug Reports

When reporting bugs, please include:

- **Clear Title**: Describe the issue concisely
- **Environment**: .NET version, OS, JsonToolkit.STJ version
- **Reproduction Steps**: Step-by-step instructions to reproduce
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Code Sample**: Minimal code that demonstrates the issue

### Feature Requests

For feature requests, please include:

- **Use Case**: Describe the problem you're trying to solve
- **Proposed Solution**: Your idea for how to solve it
- **Alternatives**: Other solutions you've considered
- **Examples**: Code examples of how the feature would be used

## 📚 Documentation

### API Documentation

- All public APIs must have XML documentation
- Include usage examples in XML docs where helpful
- Document exceptions that can be thrown

### README Updates

- Update the README if your changes affect:
  - Installation instructions
  - Usage examples
  - Feature list
  - Performance characteristics

## 🔄 Release Process

Releases are automated through our CI/CD pipeline:

1. **Version Tags**: Releases are triggered by version tags (e.g., `v1.2.3`)
2. **Automated Testing**: All tests must pass across supported frameworks
3. **NuGet Publishing**: Packages are published automatically using OIDC
4. **GitHub Releases**: Release notes are generated automatically

### Semantic Versioning

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## 🤝 Community Guidelines

### Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help others learn and grow
- Celebrate diverse perspectives

### Communication

- **Issues**: Use GitHub issues for bug reports and feature requests
- **Discussions**: Use GitHub discussions for questions and general discussion
- **Pull Requests**: Use PR comments for code review discussions

## 🏆 Recognition

Contributors are recognized in several ways:

- **Contributors List**: All contributors are listed in the repository
- **Release Notes**: Significant contributions are mentioned in release notes
- **Community**: Active contributors may be invited to join the maintainer team

## 📞 Getting Help

If you need help:

1. **Check Documentation**: Review the README and API documentation
2. **Search Issues**: Look for existing issues that might answer your question
3. **Create Discussion**: Start a GitHub discussion for general questions
4. **Create Issue**: Create an issue for specific bugs or feature requests

## 🙏 Thank You

Thank you for contributing to JsonToolkit.STJ! Your contributions help make this library better for everyone in the .NET community.

---

**Happy Coding!** 🎉