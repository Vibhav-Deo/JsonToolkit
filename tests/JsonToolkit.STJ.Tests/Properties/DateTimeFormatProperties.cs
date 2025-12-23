using System;
using System.Globalization;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for date/time format handling.
    /// </summary>
    public class DateTimeFormatProperties
    {
        /// <summary>
        /// Property 7: Date format round-trip consistency.
        /// Validates: Requirement 5.5
        /// </summary>
        [Property(MaxTest = 100)]
        public bool DateTime_Iso8601FormatRoundTrips(int year, int month, int day, int hour, int minute, int second)
        {
            try
            {
                if (year < 1 || year > 9999) return true;
                if (month < 1 || month > 12) return true;
                if (day < 1 || day > DateTime.DaysInMonth(year, month)) return true;
                if (hour < 0 || hour > 23) return true;
                if (minute < 0 || minute > 59) return true;
                if (second < 0 || second > 59) return true;

                var original = new DateTime(year, month, day, hour, minute, second, 0, DateTimeKind.Utc);
                var options = new JsonOptionsBuilder()
                    .WithDateTimeFormat(DateTimeFormats.Iso8601)
                    .Build();

                var json = JsonSerializer.Serialize(original, options);
                var roundTrip = JsonSerializer.Deserialize<DateTime>(json, options);

                // "O" format preserves DateTimeKind and full precision
                return original == roundTrip;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Property(MaxTest = 100)]
        public bool DateTime_DateOnlyFormatRoundTrips(int year, int month, int day)
        {
            try
            {
                if (year < 1 || year > 9999) return true;
                if (month < 1 || month > 12) return true;
                if (day < 1 || day > DateTime.DaysInMonth(year, month)) return true;

                var original = new DateTime(year, month, day);
                var options = new JsonOptionsBuilder()
                    .WithDateTimeFormat(DateTimeFormats.DateOnly)
                    .Build();

                var json = JsonSerializer.Serialize(original, options);
                var roundTrip = JsonSerializer.Deserialize<DateTime>(json, options);

                return original.Date == roundTrip.Date;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public void DateTime_Iso8601HandlesDateTimeKind()
        {
            var options = new JsonOptionsBuilder()
                .WithDateTimeFormat(DateTimeFormats.Iso8601)
                .Build();

            var localTime = new DateTime(2025, 4, 25, 17, 7, 8, DateTimeKind.Local);
            var utcTime = new DateTime(2025, 4, 25, 5, 7, 8, DateTimeKind.Utc);
            var unspecifiedTime = new DateTime(2025, 4, 25, 5, 7, 8, DateTimeKind.Unspecified);

            // Serialize
            var localJson = JsonSerializer.Serialize(localTime, options);
            var utcJson = JsonSerializer.Serialize(utcTime, options);
            var unspecifiedJson = JsonSerializer.Serialize(unspecifiedTime, options);

            // UTC should have Z suffix
            Assert.Contains("Z", utcJson);

            // Deserialize and verify round-trip
            var localRoundTrip = JsonSerializer.Deserialize<DateTime>(localJson, options);
            var utcRoundTrip = JsonSerializer.Deserialize<DateTime>(utcJson, options);
            var unspecifiedRoundTrip = JsonSerializer.Deserialize<DateTime>(unspecifiedJson, options);

            Assert.Equal(localTime, localRoundTrip);
            Assert.Equal(utcTime, utcRoundTrip);
            Assert.Equal(unspecifiedTime, unspecifiedRoundTrip);
        }

        [Fact]
        public void DateTime_CustomFormatWorks()
        {
            var original = new DateTime(2024, 1, 15, 10, 30, 45);
            var options = new JsonOptionsBuilder()
                .WithDateTimeFormat("yyyy-MM-dd HH:mm:ss")
                .Build();

            var json = JsonSerializer.Serialize(original, options);
            Assert.Contains("2024-01-15 10:30:45", json);

            var roundTrip = JsonSerializer.Deserialize<DateTime>(json, options);
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DateTimeOffset_Iso8601WithTimezoneRoundTrips()
        {
            var original = new DateTimeOffset(2024, 1, 15, 10, 30, 45, TimeSpan.FromHours(-5));
            var options = new JsonOptionsBuilder()
                .WithDateTimeFormat(DateTimeFormats.Iso8601WithTimezone)
                .Build();

            var json = JsonSerializer.Serialize(original, options);
            var roundTrip = JsonSerializer.Deserialize<DateTimeOffset>(json, options);

            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DateTime_InvalidFormatThrows()
        {
            var options = new JsonOptionsBuilder()
                .WithDateTimeFormat(DateTimeFormats.Iso8601)
                .Build();

            var invalidJson = "\"not-a-date\"";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(invalidJson, options));
        }

        [Property(MaxTest = 50)]
        public bool DateTime_DifferentFormatsProduceDifferentOutput(int year, int month, int day)
        {
            try
            {
                if (year < 1 || year > 9999) return true;
                if (month < 1 || month > 12) return true;
                if (day < 1 || day > DateTime.DaysInMonth(year, month)) return true;

                var date = new DateTime(year, month, day, 10, 30, 45);

                var options1 = new JsonOptionsBuilder()
                    .WithDateTimeFormat(DateTimeFormats.Iso8601)
                    .Build();

                var options2 = new JsonOptionsBuilder()
                    .WithDateTimeFormat(DateTimeFormats.DateOnly)
                    .Build();

                var json1 = JsonSerializer.Serialize(date, options1);
                var json2 = JsonSerializer.Serialize(date, options2);

                return json1 != json2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
