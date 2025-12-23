using System.Text.Json.Serialization;

namespace JsonToolkit.STJ
{
    /// <summary>
    /// Defines strategies for handling circular references during serialization.
    /// </summary>
    public enum CircularReferenceHandling
    {
        /// <summary>
        /// Throw an exception when a circular reference is detected.
        /// </summary>
        Error,

        /// <summary>
        /// Ignore circular references by not serializing them.
        /// </summary>
        Ignore,

        /// <summary>
        /// Preserve circular references using $id and $ref metadata.
        /// </summary>
        Preserve
    }

    /// <summary>
    /// Configuration options for circular reference handling.
    /// </summary>
    public class CircularReferenceOptions
    {
        /// <summary>
        /// Gets or sets the circular reference handling strategy.
        /// </summary>
        public CircularReferenceHandling Handling { get; set; } = CircularReferenceHandling.Error;

        /// <summary>
        /// Gets the appropriate ReferenceHandler for the configured strategy.
        /// </summary>
        internal ReferenceHandler? GetReferenceHandler()
        {
            return Handling switch
            {
                CircularReferenceHandling.Preserve => ReferenceHandler.Preserve,
                CircularReferenceHandling.Ignore => ReferenceHandler.IgnoreCycles,
                _ => null
            };
        }
    }
}
