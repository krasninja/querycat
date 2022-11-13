using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Order iterator.
/// </summary>
internal sealed class OrderRowsIterator : IRowsIterator
{
    private readonly VariantValueFuncData _data;
    private readonly OrderBy[] _orders;
    private readonly OrderColumnsIndex _orderIndex;
    private readonly IRowsIterator _orderIndexIterator;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;

    private bool _isInitialized;

    internal record OrderBy(FuncUnit Func, OrderDirection Direction, DataType DataType);

    /// <inheritdoc />
    public Column[] Columns => _rowsFrameIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    internal RowsFrame RowsFrame => _rowsFrame;

    public OrderRowsIterator(
        VariantValueFuncData data,
        OrderBy[] orders)
    {
        _data = data;
        _orders = orders;

        var columns = _data.RowsIterator.Columns.Union(
            orders.Select((i, index) => new Column($"__order{index}", i.DataType)));
        _rowsFrame = new RowsFrame(columns.ToArray());
        _rowsFrameIterator = _rowsFrame.GetIterator();

        _orderIndex = new OrderColumnsIndex(
            _rowsFrameIterator,
            orders.Select(o => o.Direction).ToArray(),
            orders.Select((o, index) => index + _data.RowsIterator.Columns.Length).ToArray()
        );
        _orderIndexIterator = _orderIndex.GetOrderIterator();
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

        return _orderIndexIterator.MoveNext();
    }

    private void CopyRowIteratorToFrame()
    {
        var row = new Row(_rowsFrame);
        while (_data.RowsIterator.MoveNext())
        {
            for (int i = 0; i < _data.RowsIterator.Columns.Length; i++)
            {
                row[i] = _data.RowsIterator.Current[i];
            }
            for (int i = 0; i < _orders.Length; i++)
            {
                row[i + _data.RowsIterator.Columns.Length] = _orders[i].Func.Invoke();
            }
            _rowsFrame.AddRow(row);
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
        stringBuilder.AppendRowsIteratorsWithIndent("Order", _data.RowsIterator);
    }
}
