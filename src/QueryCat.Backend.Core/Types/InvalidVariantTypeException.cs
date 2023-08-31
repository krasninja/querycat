namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The exception occurs when <see cref="Types.VariantValue" /> is attempted to use with invalid data type.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class InvalidVariantTypeException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fromType">Source type.</param>
    /// <param name="toType">Destination type.</param>
    public InvalidVariantTypeException(DataType fromType, DataType toType) :
        base($"Cannot convert variant type from {fromType} to {toType}.")
    {
    }
}
