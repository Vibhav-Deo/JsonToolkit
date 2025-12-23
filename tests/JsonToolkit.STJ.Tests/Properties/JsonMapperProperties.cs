using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    public class JsonMapperProperties
    {
        [Property]
        public Property JsonMapper_SimpleMapping_PreservesValues(string value)
        {
            return (value != null).ToProperty().And(() =>
            {
                var source = new { Name = value };
                var mapper = new JsonMapper();

                var result = mapper.Map<object, TestTarget>(source);

                return result != null && result.Name == value;
            });
        }

        [Property]
        public Property JsonMapper_PropertyMapping_RenamesCorrectly(string value)
        {
            return (value != null).ToProperty().And(() =>
            {
                var source = new { OldName = value };
                var mapper = new JsonMapper()
                    .MapProperty("OldName", "NewName");

                var json = JsonSerializer.Serialize(source);
                var element = JsonDocument.Parse(json).RootElement;
                var result = mapper.Map<object, TestTargetRenamed>(source);

                return result != null && result.NewName == value;
            });
        }

        [Property]
        public Property JsonMapper_Transformation_AppliesFunction(int value)
        {
            return true.ToProperty().And(() =>
            {
                var source = new { Number = value };
                var mapper = new JsonMapper()
                    .Transform("Number", x => Convert.ToDouble(x) * 2);

                var result = mapper.Map<object, TestTargetNumber>(source);

                return result != null && Math.Abs(result.Number - (value * 2)) < 0.001;
            });
        }

        [Property]
        public Property JsonMapper_MultipleProperties_AllMapped(string name, int age)
        {
            return (name != null).ToProperty().And(() =>
            {
                var source = new { Name = name, Age = age };
                var mapper = new JsonMapper();

                var result = mapper.Map<object, TestTargetMultiple>(source);

                return result != null && result.Name == name && result.Age == age;
            });
        }

        [Property]
        public Property JsonMapper_NullSource_ThrowsException()
        {
            return true.ToProperty().And(() =>
            {
                var mapper = new JsonMapper();

                try
                {
                    mapper.Map<TestTarget, TestTarget>(null);
                    return false;
                }
                catch (ArgumentNullException)
                {
                    return true;
                }
            });
        }

        [Property]
        public Property MappingConfiguration_ForMember_MapsCorrectly(string value)
        {
            return (value != null).ToProperty().And(() =>
            {
                var source = new { Source = value };
                var config = new MappingConfiguration<object, TestTargetRenamed>()
                    .ForMember("Source", "NewName");

                var result = config.Map(source);

                return result != null && result.NewName == value;
            });
        }

        [Property]
        public Property MappingConfiguration_WithTransform_AppliesTransformation(int value)
        {
            return true.ToProperty().And(() =>
            {
                var source = new { Value = value };
                var config = new MappingConfiguration<object, TestTargetNumber>()
                    .ForMember("Value", "Number")
                    .WithTransform("Value", x => Convert.ToDouble(x) + 10);

                var result = config.Map(source);

                return result != null && Math.Abs(result.Number - (value + 10)) < 0.001;
            });
        }

        [Property]
        public Property JsonMapper_UnmappedProperties_UseOriginalName(string name, string extra)
        {
            return (name != null && extra != null).ToProperty().And(() =>
            {
                var source = new { Name = name, Extra = extra };
                var mapper = new JsonMapper();

                var result = mapper.Map<object, TestTargetWithExtra>(source);

                return result != null && result.Name == name && result.Extra == extra;
            });
        }

        [Property]
        public Property JsonMapper_BooleanValues_PreservedCorrectly(bool value)
        {
            return true.ToProperty().And(() =>
            {
                var source = new { Flag = value };
                var mapper = new JsonMapper();

                var result = mapper.Map<object, TestTargetBool>(source);

                return result != null && result.Flag == value;
            });
        }

        [Property]
        public Property JsonMapper_ChainedMappings_AllApplied(string name, int age)
        {
            return (name != null).ToProperty().And(() =>
            {
                var source = new { OldName = name, OldAge = age };
                var mapper = new JsonMapper()
                    .MapProperty("OldName", "Name")
                    .MapProperty("OldAge", "Age");

                var result = mapper.Map<object, TestTargetMultiple>(source);

                return result != null && result.Name == name && result.Age == age;
            });
        }

        public class TestTarget
        {
            public string Name { get; set; }
        }

        public class TestTargetRenamed
        {
            public string NewName { get; set; }
        }

        public class TestTargetNumber
        {
            public double Number { get; set; }
        }

        public class TestTargetMultiple
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class TestTargetWithExtra
        {
            public string Name { get; set; }
            public string Extra { get; set; }
        }

        public class TestTargetBool
        {
            public bool Flag { get; set; }
        }
    }
}
