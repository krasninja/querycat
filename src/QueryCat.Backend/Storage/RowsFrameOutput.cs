using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Allows to write to the <see cref="RowsFrame" /> using <see cref="IRowsOutput" />.
/// </summary>
public class RowsFrameOutput : RowsOutput
{
    private readonly RowsFrame _rowsFrame;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsFrame">Instance of <see cref="RowsFrame" />.</param>
    public RowsFrameOutput(RowsFrame rowsFrame)
    {
        _rowsFrame = rowsFrame;
    }

    /// <inheritdoc />
    public override void Open()
    {
    }

    /// <inheritdoc />
    public override void Close()
    {
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        _rowsFrame.AddRow(values);
    }
}
