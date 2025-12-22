using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Provides debugging utilities for JSON converter identification and analysis.
    /// </summary>
    public static class ConverterDebugger
    {
        /// <summary>
        /// Identifies which converter would be used for a specific type with given options.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="options">The JsonSerializerOptions to analyze.</param>
        /// <returns>Information about the converter that would be used.</returns>
        public static ConverterIdentificationResult IdentifyConverter(Type type, JsonSerializerOptions options)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            try
            {
                // Find the converter that would be used
                var converter = FindConverterForType(type, options);
                
                if (converter == null)
                {
                    return new ConverterIdentificationResult
                    {
                        Type = type,
                        ConverterFound = false,
                        Message = $"No converter found for type '{type.Name}'. System.Text.Json will use default serialization."
                    };
                }

                var converterType = converter.GetType();
                var isToolkitConverter = IsToolkitConverter(converter);
                var precedence = GetConverterPrecedence(converter);

                return new ConverterIdentificationResult
                {
                    Type = type,
                    ConverterFound = true,
                    Converter = converter,
                    ConverterType = converterType,
                    ConverterName = converterType.Name,
                    IsToolkitConverter = isToolkitConverter,
                    Precedence = precedence,
                    Message = $"Using converter '{converterType.Name}' for type '{type.Name}'" +
                             (isToolkitConverter ? $" (JsonToolkit.STJ converter with precedence {precedence})" : " (System converter)")
                };
            }
            catch (Exception ex)
            {
                return new ConverterIdentificationResult
                {
                    Type = type,
                    ConverterFound = false,
                    Error = ex,
                    Message = $"Error identifying converter for type '{type.Name}': {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Analyzes all converters in the given options for potential conflicts.
        /// </summary>
        /// <param name="options">The JsonSerializerOptions to analyze.</param>
        /// <returns>Analysis results including any conflicts found.</returns>
        public static ConverterAnalysisResult AnalyzeConverters(JsonSerializerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var result = new ConverterAnalysisResult();
            var convertersByType = new Dictionary<Type, List<JsonConverter>>();

            // Group converters by target type
            foreach (var converter in options.Converters)
            {
                var targetType = GetConverterTargetType(converter);
                if (targetType != null)
                {
                    if (!convertersByType.TryGetValue(targetType, out var converters))
                    {
                        converters = new List<JsonConverter>();
                        convertersByType[targetType] = converters;
                    }
                    converters.Add(converter);
                }
            }

            // Analyze each type for conflicts
            foreach (var kvp in convertersByType)
            {
                var type = kvp.Key;
                var converters = kvp.Value;

                if (converters.Count > 1)
                {
                    // Multiple converters for the same type - potential conflict
                    var toolkitConverters = converters.Where(IsToolkitConverter).ToList();
                    var systemConverters = converters.Where(c => !IsToolkitConverter(c)).ToList();

                    if (toolkitConverters.Count > 1)
                    {
                        // Check precedence among toolkit converters
                        var precedences = toolkitConverters.Select(GetConverterPrecedence).Distinct().ToList();
                        if (precedences.Count == 1)
                        {
                            // Same precedence - this is a conflict
                            result.Conflicts.Add(new ConverterConflict
                            {
                                Type = type,
                                ConflictingConverters = toolkitConverters,
                                ConflictType = ConflictType.SamePrecedence,
                                Message = $"Multiple JsonToolkit.STJ converters with same precedence ({precedences[0]}) for type '{type.Name}'"
                            });
                        }
                    }

                    if (systemConverters.Count > 1)
                    {
                        result.Conflicts.Add(new ConverterConflict
                        {
                            Type = type,
                            ConflictingConverters = systemConverters,
                            ConflictType = ConflictType.MultipleSystemConverters,
                            Message = $"Multiple system converters registered for type '{type.Name}'"
                        });
                    }
                }

                result.ConvertersByType[type] = converters;
            }

            result.TotalConverters = options.Converters.Count;
            result.ToolkitConverters = options.Converters.Count(IsToolkitConverter);
            result.SystemConverters = result.TotalConverters - result.ToolkitConverters;

            return result;
        }

        /// <summary>
        /// Gets detailed information about a specific converter.
        /// </summary>
        /// <param name="converter">The converter to analyze.</param>
        /// <returns>Detailed information about the converter.</returns>
        public static ConverterDetails GetConverterDetails(JsonConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            var details = new ConverterDetails
            {
                Converter = converter,
                ConverterType = converter.GetType(),
                TargetType = GetConverterTargetType(converter),
                IsToolkitConverter = IsToolkitConverter(converter),
                Precedence = GetConverterPrecedence(converter)
            };

            // Get additional details based on converter type
            switch (converter)
            {
                case SimpleJsonConverter<object> simple:
                    details.ConverterCategory = "SimpleJsonConverter";
                    details.DebugInfo = simple.GetDebugInfo();
                    details.CanRead = true;
                    details.CanWrite = true;
                    break;

                case ReadOnlyJsonConverter<object> readOnly:
                    details.ConverterCategory = "ReadOnlyJsonConverter";
                    details.DebugInfo = readOnly.GetDebugInfo();
                    details.CanRead = true;
                    details.CanWrite = false;
                    break;

                case WriteOnlyJsonConverter<object> writeOnly:
                    details.ConverterCategory = "WriteOnlyJsonConverter";
                    details.DebugInfo = writeOnly.GetDebugInfo();
                    details.CanRead = false;
                    details.CanWrite = true;
                    break;

                default:
                    details.ConverterCategory = "SystemConverter";
                    details.DebugInfo = $"System converter: {converter.GetType().Name}";
                    details.CanRead = true; // Assume system converters can read/write
                    details.CanWrite = true;
                    break;
            }

            return details;
        }

        private static JsonConverter? FindConverterForType(Type type, JsonSerializerOptions options)
        {
            // This is a simplified version - the actual System.Text.Json logic is more complex
            return options.Converters.FirstOrDefault(c => c.CanConvert(type));
        }

        private static Type? GetConverterTargetType(JsonConverter converter)
        {
            var converterType = converter.GetType();
            
            while (converterType != null && converterType != typeof(object))
            {
                if (converterType.IsGenericType)
                {
                    var genericTypeDef = converterType.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(JsonConverter<>) ||
                        genericTypeDef == typeof(SimpleJsonConverter<>) ||
                        genericTypeDef == typeof(ReadOnlyJsonConverter<>) ||
                        genericTypeDef == typeof(WriteOnlyJsonConverter<>))
                    {
                        return converterType.GetGenericArguments()[0];
                    }
                }
                
                converterType = converterType.BaseType;
            }

            return null;
        }

        private static bool IsToolkitConverter(JsonConverter converter)
        {
            return converter is SimpleJsonConverter<object> ||
                   converter is ReadOnlyJsonConverter<object> ||
                   converter is WriteOnlyJsonConverter<object> ||
                   converter.GetType().Namespace?.StartsWith("JsonToolkit.STJ") == true;
        }

        private static int GetConverterPrecedence(JsonConverter converter)
        {
            return converter switch
            {
                SimpleJsonConverter<object> simple => simple.Precedence,
                ReadOnlyJsonConverter<object> readOnly => readOnly.Precedence,
                WriteOnlyJsonConverter<object> writeOnly => writeOnly.Precedence,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Result of converter identification for a specific type.
    /// </summary>
    public class ConverterIdentificationResult
    {
        public Type Type { get; set; } = null!;
        public bool ConverterFound { get; set; }
        public JsonConverter? Converter { get; set; }
        public Type? ConverterType { get; set; }
        public string? ConverterName { get; set; }
        public bool IsToolkitConverter { get; set; }
        public int Precedence { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// Result of analyzing all converters in JsonSerializerOptions.
    /// </summary>
    public class ConverterAnalysisResult
    {
        public Dictionary<Type, List<JsonConverter>> ConvertersByType { get; set; } = new();
        public List<ConverterConflict> Conflicts { get; set; } = new();
        public int TotalConverters { get; set; }
        public int ToolkitConverters { get; set; }
        public int SystemConverters { get; set; }
        public bool HasConflicts => Conflicts.Count > 0;
    }

    /// <summary>
    /// Represents a converter conflict.
    /// </summary>
    public class ConverterConflict
    {
        public Type Type { get; set; } = null!;
        public List<JsonConverter> ConflictingConverters { get; set; } = new();
        public ConflictType ConflictType { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of converter conflicts.
    /// </summary>
    public enum ConflictType
    {
        SamePrecedence,
        MultipleSystemConverters,
        TypeMismatch
    }

    /// <summary>
    /// Detailed information about a converter.
    /// </summary>
    public class ConverterDetails
    {
        public JsonConverter Converter { get; set; } = null!;
        public Type ConverterType { get; set; } = null!;
        public Type? TargetType { get; set; }
        public bool IsToolkitConverter { get; set; }
        public int Precedence { get; set; }
        public string ConverterCategory { get; set; } = string.Empty;
        public string DebugInfo { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
    }
}