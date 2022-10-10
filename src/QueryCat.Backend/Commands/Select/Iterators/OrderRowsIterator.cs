using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;
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

    internal record OrderBy(FuncUnit Func, OrderDirection Direction);

    /// <inheritdoc />
    public Column[] Columns => _rowsFrameIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    public OrderRowsIterator(
        VariantValueFuncData data,
        OrderBy[] orders)
    {
        _data = data;
        _orders = orders;

        _rowsFrame = new RowsFrame(_data.RowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();

        _orderIndex = new OrderColumnsIndex(
            _rowsFrameIterator,
            orders.Select(o => o.Direction),
            orders.Select(o => o.Func)
        );
        _orderIndexIterator = _orderIndex.GetOrderIterator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            _data.RowsIterator.ToFrame(_rowsFrame);
            _orderIndex.Rebuild();
            _isInitialized = true;
        }

        return _orderIndexIterator.MoveNext();
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
