namespace JsonToolkit.STJ;

/// <summary>
/// Configuration options for case-insensitive property matching.
/// </summary>
public class CaseInsensitivePropertyOptions
{
        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when ambiguous property matches are found.
        /// When true, throws an exception if multiple properties could match a JSON property name.
        /// When false, uses the first match found.
        /// </summary>
        public bool ThrowOnAmbiguousMatch { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to log warnings for ambiguous matches.
        /// Only applies when ThrowOnAmbiguousMatch is false.
        /// </summary>
        public bool LogAmbiguousMatches { get; set; } = true;

        /// <summary>
        /// Gets or sets the comparison type to use for case-insensitive matching.
        /// </summary>
        public StringComparison ComparisonType { get; set; } = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Gets or sets a value indicating whether to enable strict mode for property matching.
        /// When true, enables strict validation and error checking.
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception on ambiguous property matches.
        /// This is an alias for ThrowOnAmbiguousMatch for backward compatibility.
        /// </summary>
        public bool ThrowOnAmbiguity
        {
            get => ThrowOnAmbiguousMatch;
            set => ThrowOnAmbiguousMatch = value;
        }
    }
