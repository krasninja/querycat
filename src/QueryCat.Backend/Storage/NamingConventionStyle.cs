namespace QueryCat.Backend.Storage;

/// <summary>
/// Columns naming convention styles.
/// </summary>
public enum NamingConventionStyle
{
    /// <summary>
    /// Keep naming as is.
    /// </summary>
    Keep,

    /// <summary>
    /// Snake case (the_sample_name).
    /// </summary>
    SnakeCase,

    /// <summary>
    /// Camel case (theSampleName).
    /// </summary>
    CamelCase,

    /// <summary>
    /// Pascal case (TheSampleName).
    /// </summary>
    PascalCase,
}
