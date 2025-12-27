namespace JsonToolkit.STJ;

/// <summary>
/// Options for configuring null value handling during serialization and deserialization.
/// </summary>
public class NullHandlingOptions
{
    /// <summary>
    /// Gets or sets whether to distinguish between missing properties and null values.
    /// Default is true.
    /// </summary>
    public bool DistinguishMissingFromNull { get; set; } = true;

    /// <summary>
    /// Gets or sets the null serialization behavior.
    /// </summary>
    public NullSerializationBehavior SerializationBehavior { get; set; } = NullSerializationBehavior.Include;

    /// <summary>
    /// Gets or sets whether to skip serializing properties with default values.
    /// Default is false.
    /// </summary>
    public bool SkipDefaultValues { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to validate nullable reference type annotations.
    /// Default is true.
    /// </summary>
    public bool ValidateNullability { get; set; } = true;
}

/// <summary>
/// Defines how null values should be serialized.
/// </summary>
public enum NullSerializationBehavior
{
    /// <summary>
    /// Always include null values in serialized output.
    /// </summary>
    Include,

    /// <summary>
    /// Omit null values from serialized output.
    /// </summary>
    Omit,

    /// <summary>
    /// Include null values only when they differ from the default value.
    /// </summary>
    Conditional
}
