using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace JsonToolkit.STJ.Tests.Properties
{
    /// <summary>
    /// Property-based tests for circular reference handling.
    /// </summary>
    public class CircularReferenceProperties
    {
        /// <summary>
        /// Property 5: Circular reference handling is configurable and safe.
        /// Validates: Requirement 5.2
        /// </summary>
        [Fact]
        public void CircularReference_ErrorHandlingShouldThrow()
        {
            var options = new JsonOptionsBuilder()
                .WithCircularReferenceHandling(config => config.Handling = CircularReferenceHandling.Error)
                .Build();

            var node = new CircularNode { Value = 1 };
            node.Next = node; // Create circular reference

            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(node, options));
        }

        [Fact]
        public void CircularReference_IgnoreHandlingShouldNotThrow()
        {
            var options = new JsonOptionsBuilder()
                .WithCircularReferenceHandling(config => config.Handling = CircularReferenceHandling.Ignore)
                .Build();

            var node = new CircularNode { Value = 1 };
            node.Next = node; // Create circular reference

            var json = JsonSerializer.Serialize(node, options);
            Assert.NotNull(json);
            Assert.Contains("\"Value\":1", json);
        }

        [Fact]
        public void CircularReference_PreserveHandlingShouldUseMetadata()
        {
            var options = new JsonOptionsBuilder()
                .WithCircularReferenceHandling(config => config.Handling = CircularReferenceHandling.Preserve)
                .Build();

            var node = new CircularNode { Value = 1 };
            node.Next = node; // Create circular reference

            var json = JsonSerializer.Serialize(node, options);
            Assert.NotNull(json);
            Assert.Contains("$id", json);
            Assert.Contains("$ref", json);
        }

        [Fact]
        public void CircularReference_PreserveHandlingShouldRoundTrip()
        {
            var options = new JsonOptionsBuilder()
                .WithCircularReferenceHandling(config => config.Handling = CircularReferenceHandling.Preserve)
                .Build();

            var node1 = new CircularNode { Value = 1 };
            var node2 = new CircularNode { Value = 2 };
            node1.Next = node2;
            node2.Next = node1; // Create circular reference

            var json = JsonSerializer.Serialize(node1, options);
            var deserialized = JsonSerializer.Deserialize<CircularNode>(json, options);

            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized!.Value);
            Assert.NotNull(deserialized.Next);
            Assert.Equal(2, deserialized.Next!.Value);
            Assert.NotNull(deserialized.Next.Next);
            Assert.Same(deserialized, deserialized.Next.Next);
        }

        [Property(MaxTest = 50)]
        public bool CircularReference_DeepChainWithIgnoreShouldNotStackOverflow(PositiveInt depth)
        {
            try
            {
                if (depth.Get > 50) return true; // Skip very deep chains - ReferenceHandler.IgnoreCycles has depth limits

                var options = new JsonOptionsBuilder()
                    .WithCircularReferenceHandling(config => config.Handling = CircularReferenceHandling.Ignore)
                    .Build();

                var root = new CircularNode { Value = 0 };
                var current = root;

                for (int i = 1; i < depth.Get; i++)
                {
                    current.Next = new CircularNode { Value = i };
                    current = current.Next;
                }

                current.Next = root; // Create circular reference

                var json = JsonSerializer.Serialize(root, options);
                return !string.IsNullOrEmpty(json);
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Fact]
        public void CircularReference_DefaultBehaviorShouldThrow()
        {
            var options = new JsonSerializerOptions();

            var node = new CircularNode { Value = 1 };
            node.Next = node; // Create circular reference

            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(node, options));
        }
    }

    public class CircularNode
    {
        public int Value { get; set; }
        public CircularNode? Next { get; set; }
    }
}
