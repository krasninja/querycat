using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Order iterator.
/// </summary>
internal sealed class OrderRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private readonly OrderByData[] _orders;
    private readonly OrderColumnsIndex _orderIndex;
    private readonly ICursorRowsIterator _orderIndexIterator;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private readonly RowsFrame _orderRowsFrame;

    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    internal RowsFrame RowsFrame => _rowsFrame;

    public OrderRowsIterator(IExecutionThread thread, IRowsIterator rowsIterator, OrderByData[] orders)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        _orders = orders;
        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();

        var orderColumns = orders.Select((i, index) => new Column($"__order{index}", i.Func.OutputType));
        _orderRowsFrame = new RowsFrame(orderColumns.ToArray());

        _orderIndex = new OrderColumnsIndex(
            _thread,
            _orderRowsFrame.GetIterator(),
            orders.Select(
                (o, index) => new OrderColumnData(index, o.Direction, o.NullOrder))
                .ToArray()
        );
        _orderIndexIterator = _orderIndex.GetOrderIterator();
    }

    private async ValueTask CopyRowIteratorToFrameAsync(CancellationToken cancellationToken)
    {
        var row = new Row(_rowsFrame);
        var orderRow = new Row(_orderRowsFrame);
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            for (var i = 0; i < _rowsIterator.Columns.Length; i++)
            {
                row[i] = _rowsIterator.Current[i];
            }
            _rowsFrame.AddRow(row);
            for (var i = 0; i < _orders.Length; i++)
            {
                orderRow[i] = await _orders[i].Func.InvokeAsync(_thread, cancellationToken);
            }
            _orderRowsFrame.AddRow(orderRow);
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await CopyRowIteratorToFrameAsync(cancellationToken);
            await _orderIndex.RebuildAsync(cancellationToken);
            _isInitialized = true;
        }

        var hasData = await _orderIndexIterator.MoveNextAsync(cancellationToken);
        if (hasData)
        {
            _rowsFrameIterator.Seek(_orderIndexIterator.Position, CursorSeekOrigin.Begin);
        }
        return hasData;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsFrameIterator.ResetAsync(cancellationToken);
        _rowsFrame.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Order", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
