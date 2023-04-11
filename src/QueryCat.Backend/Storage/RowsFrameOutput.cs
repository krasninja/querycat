using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Allows to write to the <see cref="RowsFrame" /> using <see cref="IRowsOutput" />.
/// </summary>
public class RowsFrameOutput : RowsOutput
{
    private readonly RowsFrame _rowsFrame;

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
    protected override void OnWrite(Row row)
    {
        _rowsFrame.AddRow(row);
    }
}
