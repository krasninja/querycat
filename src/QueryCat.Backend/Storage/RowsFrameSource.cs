using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Allows to read/write from/to the <see cref="RowsFrame" /> using <see cref="IRowsOutput"  />,
/// <see cref="IRowsInput" /> and <see cref="IRowsInputDelete" />.
/// </summary>
public class RowsFrameSource : RowsOutput, IRowsInputDelete
{
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _frameIterator;
    private bool _deletedCurrentRowMark;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    /// <inheritdoc />
    public string[] UniqueKey { get; } = [];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsFrame">Instance of <see cref="RowsFrame" />.</param>
    public RowsFrameSource(RowsFrame rowsFrame)
    {
        _rowsFrame = rowsFrame;
        _frameIterator = new RowsFrameIterator(rowsFrame);
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _frameIterator.ResetAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
        _rowsFrame.AddRow(values);
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var hasData = _frameIterator.HasData;
        value = hasData ? _frameIterator.Current[columnIndex] : VariantValue.Null;
        return hasData ? ErrorCode.OK : ErrorCode.NoData;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        _deletedCurrentRowMark = false;
        return _frameIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ErrorCode> DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_deletedCurrentRowMark)
        {
            return ValueTask.FromResult(ErrorCode.Deleted);
        }
        var removed = _rowsFrame.RemoveRow(_frameIterator.Position);
        if (removed)
        {
            _deletedCurrentRowMark = true;
        }
        return ValueTask.FromResult(removed ? ErrorCode.OK : ErrorCode.NoData);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("RowsFrame");
    }
}
