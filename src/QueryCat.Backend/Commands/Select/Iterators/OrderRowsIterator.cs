using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Order iterator.
/// </summary>
internal sealed class OrderRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private readonly OrderBy[] _orders;
    private readonly OrderColumnsIndex _orderIndex;
    private readonly ICursorRowsIterator _orderIndexIterator;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private readonly RowsFrame _orderRowsFrame;

    private bool _isInitialized;

    internal record OrderBy(IFuncUnit Func, OrderDirection Direction, NullOrder NullOrder, DataType DataType);

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    internal RowsFrame RowsFrame => _rowsFrame;

    public OrderRowsIterator(IRowsIterator rowsIterator, OrderBy[] orders)
    {
        _rowsIterator = rowsIterator;
        _orders = orders;
        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();

        var orderColumns = orders.Select((i, index) => new Column($"__order{index}", i.DataType));
        _orderRowsFrame = new RowsFrame(orderColumns.ToArray());

        _orderIndex = new OrderColumnsIndex(
            _orderRowsFrame.GetIterator(),
            orders.Select(o => o.Direction).ToArray(),
            orders.Select(o => o.NullOrder).ToArray(),
            orders.Select((_, index) => index).ToArray()
        );
        _orderIndexIterator = (ICursorRowsIterator)_orderIndex.GetOrderIterator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            CopyRowIteratorToFrame();
            _orderIndex.Rebuild();
            _isInitialized = true;
        }

        var hasData = _orderIndexIterator.MoveNext();
        if (hasData)
        {
            _rowsFrameIterator.Seek(_orderIndexIterator.Position, CursorSeekOrigin.Begin);
        }
        return hasData;
    }

    private void CopyRowIteratorToFrame()
    {
        var row = new Row(_rowsFrame);
        var orderRow = new Row(_orderRowsFrame);
        while (_rowsIterator.MoveNext())
        {
            for (int i = 0; i < _rowsIterator.Columns.Length; i++)
            {
                row[i] = _rowsIterator.Current[i];
            }
            _rowsFrame.AddRow(row);
            for (int i = 0; i < _orders.Length; i++)
            {
                orderRow[i] = _orders[i].Func.Invoke();
            }
            _orderRowsFrame.AddRow(orderRow);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsFrameIterator.Reset();
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
