using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The special custom column type that is added automatically for some rows inputs.
/// For example, it might be file name or row number.
/// </summary>
public sealed class VirtualColumn : Column
{
    /// <inheritdoc />
    public VirtualColumn(string name, DataType dataType, string? description = null) : base(name, dataType, description)
    {
    }

    /// <inheritdoc />
    public VirtualColumn(Column column) : base(column)
    {
    }
}
