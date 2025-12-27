using System.Linq;
using System.Text;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ;

/// <summary>
/// Provides comprehensive error diagnostics and debugging utilities for JsonToolkit.STJ operations.
/// </summary>
public static class ErrorDiagnostics
{
    /// <summary>
    /// Analyzes an exception and provides detailed diagnostic information.
    /// </summary>
    /// <param name="exception">The exception to analyze.</param>
    /// <param name="options">The JsonSerializerOptions that were in use when the error occurred.</param>
    /// <returns>Comprehensive diagnostic information about the error.</returns>
    public static ErrorDiagnosticResult AnalyzeException(Exception exception, JsonSerializerOptions? options = null)
    {
        var result = new ErrorDiagnosticResult
        {
            OriginalException = exception,
            ExceptionType = exception.GetType(),
            Message = exception.Message,
            AnalysisTimestamp = DateTime.UtcNow
        };

        // Analyze based on exception type
        switch (exception)
        {
            case JsonToolkitException toolkitEx:
                AnalyzeToolkitException(toolkitEx, result);
                break;

            case JsonException jsonEx:
                AnalyzeJsonException(jsonEx, result);
                break;

            default:
                AnalyzeGeneralException(exception, result);
                break;
        }

        // Add converter analysis if options are available
        if (options != null)
        {
            result.ConverterAnalysis = ConverterDebugger.AnalyzeConverters(options);
        }

        // Generate recommendations
        GenerateRecommendations(result);

        return result;
    }

    /// <summary>
    /// Analyzes converter errors and provides debugging information.
    /// </summary>
    /// <param name="converterName">The name of the converter that failed.</param>
    /// <param name="targetType">The type being converted.</param>
    /// <param name="operation">The operation that failed (Read/Write).</param>
    /// <param name="options">The JsonSerializerOptions in use.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    /// <returns>Diagnostic information about the converter error.</returns>
    public static ConverterErrorDiagnostic AnalyzeConverterError(
        string converterName,
        Type targetType,
        string operation,
        JsonSerializerOptions options,
        Exception? innerException = null)
    {
        var diagnostic = new ConverterErrorDiagnostic
        {
            ConverterName = converterName,
            TargetType = targetType,
            Operation = operation,
            InnerException = innerException,
            AnalysisTimestamp = DateTime.UtcNow
        };

        // Get converter identification
        var identification = ConverterDebugger.IdentifyConverter(targetType, options);
        diagnostic.ConverterIdentification = identification;

        // Check for converter conflicts
        var analysis = ConverterDebugger.AnalyzeConverters(options);
        diagnostic.HasConverterConflicts = analysis.Conflicts.Any(c => c.Type == targetType);
        diagnostic.ConflictingConverters = analysis.Conflicts
            .Where(c => c.Type == targetType)
            .SelectMany(c => c.ConflictingConverters)
            .ToList();

        // Generate specific recommendations for converter errors
        GenerateConverterRecommendations(diagnostic);

        return diagnostic;
    }

    /// <summary>
    /// Analyzes validation errors and provides debugging information.
    /// </summary>
    /// <param name="validationErrors">The validation errors to analyze.</param>
    /// <param name="targetType">The type being validated.</param>
    /// <returns>Diagnostic information about the validation errors.</returns>
    public static ValidationErrorDiagnostic AnalyzeValidationErrors(
        IEnumerable<ValidationError> validationErrors,
        Type? targetType = null)
    {
        var errors = validationErrors.ToList();
        var diagnostic = new ValidationErrorDiagnostic
        {
            ValidationErrors = errors,
            TargetType = targetType,
            ErrorCount = errors.Count,
            AnalysisTimestamp = DateTime.UtcNow
        };

        // Group errors by type and property
        diagnostic.ErrorsByType = errors.GroupBy(e => e.ErrorType).ToDictionary(g => g.Key, g => g.ToList());
        diagnostic.ErrorsByProperty = errors.GroupBy(e => e.PropertyPath).ToDictionary(g => g.Key, g => g.ToList());

        // Identify common patterns
        diagnostic.CommonPatterns = IdentifyValidationPatterns(errors);

        // Generate validation-specific recommendations
        GenerateValidationRecommendations(diagnostic);

        return diagnostic;
    }

    /// <summary>
    /// Creates a comprehensive error report for debugging purposes.
    /// </summary>
    /// <param name="exception">The exception to report on.</param>
    /// <param name="options">The JsonSerializerOptions that were in use.</param>
    /// <param name="jsonInput">The JSON input that caused the error (optional).</param>
    /// <param name="targetType">The target type being processed (optional).</param>
    /// <returns>A formatted error report.</returns>
    public static string CreateErrorReport(
        Exception exception,
        JsonSerializerOptions? options = null,
        string? jsonInput = null,
        Type? targetType = null)
    {
        var report = new StringBuilder();
        var diagnostic = AnalyzeException(exception, options);

        report.AppendLine("=== JsonToolkit.STJ Error Report ===");
        report.AppendLine($"Timestamp: {diagnostic.AnalysisTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"Exception Type: {diagnostic.ExceptionType.Name}");
        report.AppendLine($"Message: {diagnostic.Message}");
        report.AppendLine();

        // Add context information
        if (diagnostic.PropertyPath != null)
            report.AppendLine($"Property Path: {diagnostic.PropertyPath}");

        if (diagnostic.Operation != null)
            report.AppendLine($"Operation: {diagnostic.Operation}");

        if (diagnostic.LineNumber.HasValue)
            report.AppendLine($"Line: {diagnostic.LineNumber.Value + 1}");

        if (diagnostic.BytePositionInLine.HasValue)
            report.AppendLine($"Position: {diagnostic.BytePositionInLine.Value}");

        if (targetType != null)
            report.AppendLine($"Target Type: {targetType.Name}");

        report.AppendLine();

        // Add JSON input if provided
        if (!string.IsNullOrEmpty(jsonInput))
        {
            report.AppendLine("JSON Input:");
            report.AppendLine(TruncateJson(jsonInput, 500));
            report.AppendLine();
        }

        // Add converter analysis
        if (diagnostic.ConverterAnalysis != null)
        {
            report.AppendLine("Converter Analysis:");
            report.AppendLine($"  Total Converters: {diagnostic.ConverterAnalysis.TotalConverters}");
            report.AppendLine($"  Toolkit Converters: {diagnostic.ConverterAnalysis.ToolkitConverters}");
            report.AppendLine($"  System Converters: {diagnostic.ConverterAnalysis.SystemConverters}");

            if (diagnostic.ConverterAnalysis.HasConflicts)
            {
                report.AppendLine("  Converter Conflicts:");
                foreach (var conflict in diagnostic.ConverterAnalysis.Conflicts)
                {
                    report.AppendLine($"    - {conflict.Message}");
                }
            }

            report.AppendLine();
        }

        // Add recommendations
        if (diagnostic.Recommendations.Any())
        {
            report.AppendLine("Recommendations:");
            foreach (var recommendation in diagnostic.Recommendations)
            {
                report.AppendLine($"  - {recommendation}");
            }

            report.AppendLine();
        }

        // Add stack trace
        report.AppendLine("Stack Trace:");
        report.AppendLine(exception.ToString());

        return report.ToString();
    }

    private static void AnalyzeToolkitException(JsonToolkitException toolkitEx, ErrorDiagnosticResult result)
    {
        result.IsToolkitException = true;
        result.PropertyPath = toolkitEx.PropertyPath;
        result.Operation = toolkitEx.Operation;
        result.SourceElement = toolkitEx.SourceElement;

        // Analyze specific toolkit exception types
        switch (toolkitEx)
        {
            case JsonValidationException validationEx:
                result.ValidationErrors = validationEx.ValidationErrors.ToList();
                result.ErrorCategory = "Validation";
                break;

            case JsonPathException pathEx:
                result.JsonPath = pathEx.JsonPath;
                result.JsonPathPosition = pathEx.Position;
                result.ErrorCategory = "JsonPath";
                break;

            case JsonPatchException patchEx:
                result.FailedPatchOperation = patchEx.FailedOperation;
                result.PatchOperationIndex = patchEx.OperationIndex;
                result.ErrorCategory = "JsonPatch";
                break;

            default:
                result.ErrorCategory = "General";
                break;
        }
    }

    private static void AnalyzeJsonException(JsonException jsonEx, ErrorDiagnosticResult result)
    {
        result.IsSystemJsonException = true;
        result.ErrorCategory = "SystemJson";

        // Try to extract line/position information from the message
        // System.Text.Json includes this in the message format
        ExtractLinePositionFromMessage(jsonEx.Message, result);
    }

    private static void AnalyzeGeneralException(Exception exception, ErrorDiagnosticResult result)
    {
        result.ErrorCategory = "General";

        // Check if it's a known type that might indicate specific issues
        if (exception is ArgumentException)
            result.ErrorCategory = "Argument";
        else if (exception is InvalidOperationException)
            result.ErrorCategory = "InvalidOperation";
        else if (exception is NotSupportedException)
            result.ErrorCategory = "NotSupported";
    }

    private static void ExtractLinePositionFromMessage(string message, ErrorDiagnosticResult result)
    {
        // System.Text.Json error messages often contain line/position info
        // Example: "The JSON value could not be converted to System.String. Path: $.property | LineNumber: 2 | BytePositionInLine: 15."

        var lineMatch = System.Text.RegularExpressions.Regex.Match(message, @"LineNumber:\s*(\d+)");
        if (lineMatch.Success && long.TryParse(lineMatch.Groups[1].Value, out var lineNumber))
        {
            result.LineNumber = lineNumber;
        }

        var positionMatch = System.Text.RegularExpressions.Regex.Match(message, @"BytePositionInLine:\s*(\d+)");
        if (positionMatch.Success && long.TryParse(positionMatch.Groups[1].Value, out var position))
        {
            result.BytePositionInLine = position;
        }

        var pathMatch = System.Text.RegularExpressions.Regex.Match(message, @"Path:\s*([^\s|]+)");
        if (pathMatch.Success)
        {
            result.PropertyPath = pathMatch.Groups[1].Value;
        }
    }

    private static void GenerateRecommendations(ErrorDiagnosticResult result)
    {
        var recommendations = new List<string>();

        switch (result.ErrorCategory)
        {
            case "Validation":
                recommendations.Add("Check that your JSON data matches the expected schema and validation rules");
                recommendations.Add("Verify that required properties are present and have valid values");
                break;

            case "JsonPath":
                recommendations.Add("Verify that your JsonPath expression syntax is correct");
                recommendations.Add("Check that the target JSON structure matches your JsonPath query");
                break;

            case "JsonPatch":
                recommendations.Add("Ensure that patch operation paths exist in the target document");
                recommendations.Add("Verify that patch operations are applied in the correct order");
                break;

            case "SystemJson":
                recommendations.Add("Check that your JSON is well-formed and valid");
                recommendations.Add("Verify that property names and values match the expected types");
                break;

            case "Argument":
                recommendations.Add("Check that all required parameters are provided and valid");
                recommendations.Add("Verify that configuration options are set correctly");
                break;
        }

        if (result.ConverterAnalysis?.HasConflicts == true)
        {
            recommendations.Add(
                "Resolve converter conflicts by adjusting converter precedence or removing duplicate converters");
        }

        if (result.LineNumber.HasValue)
        {
            recommendations.Add($"Focus on line {result.LineNumber.Value + 1} in your JSON input");
        }

        result.Recommendations = recommendations;
    }

    private static void GenerateConverterRecommendations(ConverterErrorDiagnostic diagnostic)
    {
        var recommendations = new List<string>();

        if (diagnostic.HasConverterConflicts)
        {
            recommendations.Add(
                "Multiple converters are registered for the same type - consider removing duplicates or adjusting precedence");
        }

        if (diagnostic.Operation == "Read")
        {
            recommendations.Add("Check that your JSON structure matches what the converter expects for reading");
            recommendations.Add("Verify that the converter's ReadValue method handles all expected JSON token types");
        }
        else if (diagnostic.Operation == "Write")
        {
            recommendations.Add("Check that the object being serialized has all required properties set");
            recommendations.Add("Verify that the converter's WriteValue method handles null values appropriately");
        }

        if (diagnostic.InnerException != null)
        {
            recommendations.Add($"Inner exception: {diagnostic.InnerException.Message}");
        }

        diagnostic.Recommendations = recommendations;
    }

    private static void GenerateValidationRecommendations(ValidationErrorDiagnostic diagnostic)
    {
        var recommendations = new List<string>();

        if (diagnostic.ErrorsByType.ContainsKey("Required"))
        {
            recommendations.Add("Ensure all required properties are present in your JSON");
        }

        if (diagnostic.ErrorsByType.ContainsKey("Range"))
        {
            recommendations.Add("Check that numeric values are within the specified ranges");
        }

        if (diagnostic.ErrorsByType.ContainsKey("Length"))
        {
            recommendations.Add("Verify that string and array lengths meet the specified constraints");
        }

        if (diagnostic.ErrorsByType.ContainsKey("Pattern"))
        {
            recommendations.Add("Ensure string values match the required regular expression patterns");
        }

        diagnostic.Recommendations = recommendations;
    }

    private static List<string> IdentifyValidationPatterns(List<ValidationError> errors)
    {
        var patterns = new List<string>();

        var propertiesWithMultipleErrors = errors
            .GroupBy(e => e.PropertyPath)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (propertiesWithMultipleErrors.Any())
        {
            patterns.Add(
                $"Multiple validation errors on properties: {string.Join(", ", propertiesWithMultipleErrors)}");
        }

        var commonErrorTypes = errors
            .GroupBy(e => e.ErrorType)
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()} occurrences)")
            .ToList();

        if (commonErrorTypes.Any())
        {
            patterns.Add($"Most common error types: {string.Join(", ", commonErrorTypes)}");
        }

        return patterns;
    }

    private static string TruncateJson(string? json, int maxLength)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        if (json!.Length <= maxLength)
            return json;

        return json.Substring(0, maxLength) + "... (truncated)";
    }
}

/// <summary>
/// Comprehensive diagnostic result for error analysis.
/// </summary>
public class ErrorDiagnosticResult
{
    public Exception OriginalException { get; set; } = null!;
    public Type ExceptionType { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public string ErrorCategory { get; set; } = string.Empty;
    public bool IsToolkitException { get; set; }
    public bool IsSystemJsonException { get; set; }
    public string? PropertyPath { get; set; }
    public string? Operation { get; set; }
    public JsonElement? SourceElement { get; set; }
    public long? LineNumber { get; set; }
    public long? BytePositionInLine { get; set; }
    public string? JsonPath { get; set; }
    public int? JsonPathPosition { get; set; }
    public JsonPatchOperation? FailedPatchOperation { get; set; }
    public int? PatchOperationIndex { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public ConverterAnalysisResult? ConverterAnalysis { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; }
}

/// <summary>
/// Diagnostic information for converter errors.
/// </summary>
public class ConverterErrorDiagnostic
{
    public string ConverterName { get; set; } = string.Empty;
    public Type TargetType { get; set; } = null!;
    public string Operation { get; set; } = string.Empty;
    public Exception? InnerException { get; set; }
    public ConverterIdentificationResult? ConverterIdentification { get; set; }
    public bool HasConverterConflicts { get; set; }
    public List<JsonConverter> ConflictingConverters { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; }
}

/// <summary>
/// Diagnostic information for validation errors.
/// </summary>
public class ValidationErrorDiagnostic
{
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public Type? TargetType { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, List<ValidationError>> ErrorsByType { get; set; } = new();
    public Dictionary<string, List<ValidationError>> ErrorsByProperty { get; set; } = new();
    public List<string> CommonPatterns { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; }
}