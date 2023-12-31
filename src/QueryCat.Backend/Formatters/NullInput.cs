using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

internal sealed class NullInput : RowsInput
{
    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    /// <inheritdoc />
    public override void Open()
    {
    }

    /// <inheritdoc />
    public override void Close()
    {
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = VariantValue.Null;
        return ErrorCode.OK;
    }
}
