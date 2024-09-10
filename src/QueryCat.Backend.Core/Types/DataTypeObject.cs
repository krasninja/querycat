namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The special object just to indicate type in <see cref="VariantValue" /> struct.
/// </summary>
internal sealed class DataTypeObject
{
    /// <summary>
    /// Related type.
    /// </summary>
    public DataType DataType { get; }

    /// <summary>
    /// Type parameter.
    /// </summary>
    public string TypeParam { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dataType">Data type.</param>
    /// <param name="typeParam">Type parameter.</param>
    public DataTypeObject(DataType dataType, string? typeParam = null)
    {
        DataType = dataType;
        TypeParam = typeParam ?? string.Empty;
    }

    /// <inheritdoc />
    public override string ToString() => $"[{DataType}]";
}
