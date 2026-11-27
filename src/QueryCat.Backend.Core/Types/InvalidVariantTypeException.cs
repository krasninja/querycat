namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The exception occurs when <see cref="Types.VariantValue" /> is attempted to use with invalid data type.
/// </summary>
public class InvalidVariantTypeException : QueryCatException
{
    /// <summary>
    /// Source type.
    /// </summary>
    public DataType FromType { get; }

    /// <summary>
    /// Destination type.
    /// </summary>
    public DataType ToType { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fromType">Source type.</param>
    /// <param name="toType">Destination type.</param>
    /// <param name="innerException">Inner exception.</param>
    public InvalidVariantTypeException(DataType fromType, DataType toType, Exception? innerException) :
        base($"Cannot convert variant type from {fromType} to {toType}.")
    {
        FromType = fromType;
        ToType = toType;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fromType">Source type.</param>
    /// <param name="toType">Destination type.</param>
    public InvalidVariantTypeException(DataType fromType, DataType toType) :
        this(fromType, toType, null)
    {
    }
}
