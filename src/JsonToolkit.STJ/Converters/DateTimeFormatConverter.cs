using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonToolkit.STJ.Converters
{
    /// <summary>
    /// Converter for DateTime with configurable format patterns.
    /// </summary>
    public class DateTimeFormatConverter : JsonConverter<DateTime>
    {
        private readonly string _format;
        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Initializes a new instance with a specific format.
        /// </summary>
        public DateTimeFormatConverter(string format, IFormatProvider? formatProvider = null)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                throw new JsonException("Cannot convert null or empty string to DateTime");

            // Use RoundtripKind for "O" format to preserve DateTimeKind
            var styles = _format == "O" ? DateTimeStyles.RoundtripKind : DateTimeStyles.None;

            if (DateTime.TryParseExact(value, _format, _formatProvider, styles, out var result))
                return result;

            throw new JsonException($"Unable to parse '{value}' as DateTime with format '{_format}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, _formatProvider));
        }
    }

    /// <summary>
    /// Converter for DateTimeOffset with configurable format patterns.
    /// </summary>
    public class DateTimeOffsetFormatConverter : JsonConverter<DateTimeOffset>
    {
        private readonly string _format;
        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Initializes a new instance with a specific format.
        /// </summary>
        public DateTimeOffsetFormatConverter(string format, IFormatProvider? formatProvider = null)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                throw new JsonException("Cannot convert null or empty string to DateTimeOffset");

            if (DateTimeOffset.TryParseExact(value, _format, _formatProvider, DateTimeStyles.None, out var result))
                return result;

            throw new JsonException($"Unable to parse '{value}' as DateTimeOffset with format '{_format}'");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, _formatProvider));
        }
    }

    /// <summary>
    /// Common date/time format patterns.
    /// </summary>
    public static class DateTimeFormats
    {
        /// <summary>ISO 8601 round-trip format (handles DateTimeKind)</summary>
        public const string Iso8601 = "O";

        /// <summary>ISO 8601 with timezone (yyyy-MM-ddTHH:mm:ss.fffzzz)</summary>
        public const string Iso8601WithTimezone = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        /// <summary>Date only (yyyy-MM-dd)</summary>
        public const string DateOnly = "yyyy-MM-dd";

        /// <summary>Microsoft JSON date format (/Date(ticks)/)</summary>
        public const string MicrosoftJson = "MicrosoftJson";

        /// <summary>RFC 1123 format</summary>
        public const string Rfc1123 = "R";

        /// <summary>Unix timestamp (seconds since epoch)</summary>
        public const string UnixTimestamp = "UnixTimestamp";
    }
}
