using System;
using System.Collections.Generic;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using JsonToolkit.STJ;
using JsonToolkit.STJ.Converters;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for optional property defaults system.
    /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
    /// </summary>
    public class OptionalPropertyDefaultsProperties
    {
        /// <summary>
        /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
        /// For any object type with configured defaults, deserialization should apply defaults for missing properties,
        /// use JSON values when present, handle null values according to configuration, and work recursively for nested objects.
        /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool OptionalPropertyDefaults_ShouldApplyDefaultsForMissingProperties(
            NonEmptyString defaultName, 
            PositiveInt defaultAge, 
            bool defaultActive,
            NonEmptyString actualName,
            PositiveInt actualAge)
        {
            try
            {
                // Create default values
                var defaults = new TestPerson
                {
                    Name = defaultName.Get,
                    Age = defaultAge.Get,
                    IsActive = defaultActive,
                    Email = "default@example.com"
                };

                // Configure options with defaults
                var options = new JsonSerializerOptions()
                    .WithOptionalDefaults(defaults);

                // Test case 1: Missing properties should use defaults
                var incompleteJson = "{}";
                var result1 = JsonSerializer.Deserialize<TestPerson>(incompleteJson, options);
                
                if (result1 == null || 
                    result1.Name != defaultName.Get || 
                    result1.Age != defaultAge.Get || 
                    result1.IsActive != defaultActive ||
                    result1.Email != "default@example.com")
                    return false;

                // Test case 2: Present properties should override defaults
                var partialJson = JsonSerializer.Serialize(new { Name = actualName.Get, Age = actualAge.Get });
                var result2 = JsonSerializer.Deserialize<TestPerson>(partialJson, options);
                
                if (result2 == null ||
                    result2.Name != actualName.Get ||
                    result2.Age != actualAge.Get ||
                    result2.IsActive != defaultActive || // Should use default
                    result2.Email != "default@example.com") // Should use default
                    return false;

                // Test case 3: Complete JSON should not use any defaults
                var completeJson = JsonSerializer.Serialize(new { Name = actualName.Get, Age = actualAge.Get, IsActive = true, Email = "test@example.com" });
                var result3 = JsonSerializer.Deserialize<TestPerson>(completeJson, options);
                
                if (result3 == null ||
                    result3.Name != actualName.Get ||
                    result3.Age != actualAge.Get ||
                    result3.IsActive != true ||
                    result3.Email != "test@example.com")
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
        /// For any configuration with property-specific defaults, the system should apply defaults
        /// only to the specified properties and respect JSON values when present.
        /// **Validates: Requirements 3.1, 3.2, 3.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool OptionalPropertyDefaults_ShouldApplyPropertySpecificDefaults(
            NonEmptyString defaultEmail,
            NonEmptyString actualName,
            PositiveInt actualAge)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithOptionalDefaults<TestPerson>(defaults =>
                    {
                        defaults.SetDefault("Email", defaultEmail.Get)
                               .SetDefault("IsActive", true);
                    });

                // Test with missing Email and IsActive - should use defaults
                var json = JsonSerializer.Serialize(new { Name = actualName.Get, Age = actualAge.Get });
                var result = JsonSerializer.Deserialize<TestPerson>(json, options);

                if (result == null ||
                    result.Name != actualName.Get ||
                    result.Age != actualAge.Get ||
                    result.Email != defaultEmail.Get ||
                    result.IsActive != true)
                    return false;

                // Test with explicit Email - should override default
                var jsonWithEmail = JsonSerializer.Serialize(new { Name = actualName.Get, Age = actualAge.Get, Email = "explicit@example.com" });
                var result2 = JsonSerializer.Deserialize<TestPerson>(jsonWithEmail, options);

                if (result2 == null ||
                    result2.Name != actualName.Get ||
                    result2.Age != actualAge.Get ||
                    result2.Email != "explicit@example.com" ||
                    result2.IsActive != true) // Should still use default
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
        /// For any configuration with null handling options, the system should handle null values
        /// according to the specified configuration (ignore nulls vs respect nulls).
        /// **Validates: Requirements 3.3**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool OptionalPropertyDefaults_ShouldHandleNullValuesCorrectly(
            NonEmptyString defaultEmail,
            bool ignoreNulls)
        {
            try
            {
                var options = new JsonSerializerOptions()
                    .WithOptionalDefaults<TestPerson>(defaults =>
                    {
                        defaults.SetDefault("Email", defaultEmail.Get)
                               .WithNullHandling(ignoreNulls);
                    });

                // Test with explicit null value
                var jsonWithNull = "{\"Name\":\"Test\",\"Age\":25,\"Email\":null}";
                var result = JsonSerializer.Deserialize<TestPerson>(jsonWithNull, options);

                if (result == null)
                    return false;

                if (ignoreNulls)
                {
                    // When ignoring nulls, should use default
                    return result.Email == defaultEmail.Get;
                }
                else
                {
                    // When respecting nulls, should keep null
                    return result.Email == null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
        /// For any nested object structure with recursive defaults enabled, the system should
        /// apply defaults to nested objects as well as top-level properties.
        /// **Validates: Requirements 3.5**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool OptionalPropertyDefaults_ShouldApplyRecursiveDefaults(
            NonEmptyString defaultPersonName,
            NonEmptyString defaultAddressStreet,
            NonEmptyString actualPersonName)
        {
            try
            {
                // Configure defaults for both parent and nested objects
                var personDefaults = new TestPersonWithAddress
                {
                    Name = defaultPersonName.Get,
                    Age = 30,
                    Address = new TestAddress
                    {
                        Street = defaultAddressStreet.Get,
                        City = "Default City",
                        ZipCode = "00000"
                    }
                };

                var personOptions = new JsonSerializerOptions()
                    .WithOptionalDefaults(personDefaults);

                var addressDefaults = new TestAddress
                {
                    Street = defaultAddressStreet.Get,
                    City = "Default City",
                    ZipCode = "00000"
                };

                var addressOptions = new JsonSerializerOptions()
                    .WithOptionalDefaults(addressDefaults);

                // Combine both options (in a real scenario, this would be done more elegantly)
                var combinedOptions = new JsonSerializerOptions();
                var factory = new OptionalPropertyConverterFactory();
                factory.RegisterDefaults(personDefaults);
                factory.RegisterDefaults(addressDefaults);
                combinedOptions.Converters.Add(factory);

                // Test with missing nested properties
                var json = JsonSerializer.Serialize(new { Name = actualPersonName.Get, Address = new { } });
                var result = JsonSerializer.Deserialize<TestPersonWithAddress>(json, combinedOptions);

                if (result == null ||
                    result.Name != actualPersonName.Get ||
                    result.Age != 30 || // Should use default
                    result.Address == null ||
                    result.Address.Street != defaultAddressStreet.Get ||
                    result.Address.City != "Default City" ||
                    result.Address.ZipCode != "00000")
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// **Feature: json-toolkit-stj, Property 3: Optional property defaults are applied consistently**
        /// For any configuration where JSON values should take precedence, explicit JSON values
        /// should always override configured defaults, regardless of the default configuration.
        /// **Validates: Requirements 3.4**
        /// </summary>
        [Property(MaxTest = 100)]
        public bool OptionalPropertyDefaults_JsonValuesShouldTakePrecedence(
            NonEmptyString defaultName,
            PositiveInt defaultAge,
            NonEmptyString actualName,
            PositiveInt actualAge)
        {
            try
            {
                var defaults = new TestPerson
                {
                    Name = defaultName.Get,
                    Age = defaultAge.Get,
                    IsActive = true,
                    Email = "default@example.com"
                };

                var options = new JsonSerializerOptions()
                    .WithOptionalDefaults<TestPerson>(config =>
                    {
                        config.DefaultValue = defaults;
                        config.JsonValuesPrecedence = true; // Explicit precedence setting
                    });

                // Test that explicit JSON values override defaults
                var json = JsonSerializer.Serialize(new { Name = actualName.Get, Age = actualAge.Get, IsActive = false, Email = "actual@example.com" });
                var result = JsonSerializer.Deserialize<TestPerson>(json, options);

                if (result == null ||
                    result.Name != actualName.Get ||
                    result.Age != actualAge.Get ||
                    result.IsActive != false ||
                    result.Email != "actual@example.com")
                    return false;

                // Test that missing properties still use defaults
                var partialJson = JsonSerializer.Serialize(new { Name = actualName.Get });
                var result2 = JsonSerializer.Deserialize<TestPerson>(partialJson, options);

                if (result2 == null ||
                    result2.Name != actualName.Get ||
                    result2.Age != defaultAge.Get ||
                    result2.IsActive != true ||
                    result2.Email != "default@example.com")
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Test class for property-based testing of optional property defaults.
    /// </summary>
    public class TestPerson
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Test class with nested object for recursive defaults testing.
    /// </summary>
    public class TestPersonWithAddress
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public TestAddress? Address { get; set; }
    }

    /// <summary>
    /// Test address class for nested object testing.
    /// </summary>
    public class TestAddress
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
    }
}