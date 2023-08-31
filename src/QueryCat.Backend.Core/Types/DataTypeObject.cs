namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The special object just to indicate type in <see cref="VariantValue" /> struct.
/// </summary>
internal sealed class DataTypeObject
{
    /// <summary>
    /// Type name.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="typeName">Type name.</param>
    public DataTypeObject(string typeName)
    {
        TypeName = typeName;
    }

    /// <inheritdoc />
    public override string ToString() => $"[{TypeName}]";
}
