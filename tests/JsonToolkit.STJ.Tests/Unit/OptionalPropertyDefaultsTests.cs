using System;
using System.Text.Json;
using Xunit;
using JsonToolkit.STJ;

namespace JsonToolkit.STJ.Tests.Unit
{
    /// <summary>
    /// Unit tests for optional property defaults functionality.
    /// </summary>
    public class OptionalPropertyDefaultsTests
    {
        public class TestPerson
        {
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public string? Email { get; set; }
        }

        [Fact]
        public void WithOptionalDefaults_ShouldApplyDefaultsForMissingProperties()
        {
            // Arrange
            var defaults = new TestPerson
            {
                Name = "DefaultName",
                Age = 25,
                IsActive = true,
                Email = "default@example.com"
            };

            var options = new JsonSerializerOptions()
                .WithOptionalDefaults(defaults);

            // Act
            var result = JsonSerializer.Deserialize<TestPerson>("{}", options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DefaultName", result.Name);
            Assert.Equal(25, result.Age);
            Assert.True(result.IsActive);
            Assert.Equal("default@example.com", result.Email);
        }

        [Fact]
        public void WithOptionalDefaults_ShouldUseJsonValuesWhenPresent()
        {
            // Arrange
            var defaults = new TestPerson
            {
                Name = "DefaultName",
                Age = 25,
                IsActive = true,
                Email = "default@example.com"
            };

            var options = new JsonSerializerOptions()
                .WithOptionalDefaults(defaults);

            var json = "{\"Name\":\"ActualName\",\"Age\":30}";

            // Act
            var result = JsonSerializer.Deserialize<TestPerson>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ActualName", result.Name); // Should use JSON value
            Assert.Equal(30, result.Age); // Should use JSON value
            Assert.True(result.IsActive); // Should use default
            Assert.Equal("default@example.com", result.Email); // Should use default
        }

        [Fact]
        public void WithOptionalDefaults_PropertySpecific_ShouldWork()
        {
            // Arrange
            var options = new JsonSerializerOptions()
                .WithOptionalDefaults<TestPerson>(defaults =>
                {
                    defaults.SetDefault("Email", "property@example.com")
                           .SetDefault("IsActive", true);
                });

            var json = "{\"Name\":\"TestName\",\"Age\":35}";

            // Act
            var result = JsonSerializer.Deserialize<TestPerson>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestName", result.Name);
            Assert.Equal(35, result.Age);
            Assert.True(result.IsActive); // Should use property default
            Assert.Equal("property@example.com", result.Email); // Should use property default
        }
    }
}