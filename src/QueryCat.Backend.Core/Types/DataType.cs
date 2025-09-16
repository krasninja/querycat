namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Base types system.
/// </summary>
public enum DataType
{
    /// <summary>
    /// No return type. For example, can be used for functions with no return value.
    /// </summary>
    Void = -1,

    /// <summary>
    /// Unknown.
    /// </summary>
    Null = 0,

    /// <summary>
    /// Integer type.
    /// </summary>
    Integer = 1,

    /// <summary>
    /// Text type.
    /// </summary>
    String = 2,

    /// <summary>
    /// Float type.
    /// </summary>
    Float = 3,

    /// <summary>
    /// Date/time.
    /// </summary>
    Timestamp = 4,

    /// <summary>
    /// True or false.
    /// </summary>
    Boolean = 5,

    /// <summary>
    /// Decimal 128-bit data type.
    /// </summary>
    Numeric = 6,

    /// <summary>
    /// Time span value.
    /// </summary>
    Interval = 7,

    /// <summary>
    /// Binary Large Object.
    /// </summary>
    Blob = 8,

    /// <summary>
    /// Other object type.
    /// </summary>
    Object = 40,

    /// <summary>
    /// The type may change during evaluation. Bypass static type optimizations
    /// and check. The variant value cannot have this type.
    /// </summary>
    Dynamic = 41,

    /// <summary>
    /// Array of values.
    /// </summary>
    Array = 42,

    /// <summary>
    /// Key-values pairs.
    /// </summary>
    Map = 43,
}
