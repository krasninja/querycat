using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Indexes;

/// <summary>
/// Order columns according to expression.
/// </summary>
public class OrderColumnsIndex : IOrderIndex
{
    private int[] _rowsOrder = Array.Empty<int>();
    private readonly OrderDirection[] _directions;
    private readonly IFuncUnit[] _valueGetters;
    private readonly int[] _columnIndexes;

    private ICursorRowsIterator RowsFrameIterator { get; }

    private class RowsComparer : IComparer<int>
    {
        private readonly OrderColumnsIndex _orderColumnsIndex;

        private readonly VariantValue[] _values1;
        private readonly VariantValue[] _values2;

        private readonly int[] _greaterValues;
        private readonly int[] _lessValues;

        private readonly VariantValue.BinaryFunction[] _greaterFunctions;
        private readonly VariantValue.BinaryFunction[] _lessFunctions;

        public RowsComparer(OrderColumnsIndex orderColumnsIndex)
        {
            _orderColumnsIndex = orderColumnsIndex;

            _values1 = new VariantValue[_orderColumnsIndex._valueGetters.Length];
            _values2 = new VariantValue[_orderColumnsIndex._valueGetters.Length];

            _greaterValues = _orderColumnsIndex._directions
                .Select(d => d == OrderDirection.Ascending ? 1 : -1).ToArray();
            _lessValues = _orderColumnsIndex._directions
                .Select(d => d == OrderDirection.Ascending ? -1 : 1).ToArray();
            var columnsTypes = _orderColumnsIndex._columnIndexes
                .Select(i => _orderColumnsIndex.RowsFrameIterator.Columns[i].DataType).ToArray();
            _greaterFunctions = columnsTypes.Select(t => VariantValue.GetGreaterDelegate(t, t)).ToArray();
            _lessFunctions = columnsTypes.Select(t => VariantValue.GetLessDelegate(t, t)).ToArray();
        }

        private void FillValues(VariantValue[] values, int rowIndex)
        {
            _orderColumnsIndex.RowsFrameIterator.Seek(rowIndex, CursorSeekOrigin.Begin);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _orderColumnsIndex._valueGetters[i].Invoke();
            }
        }

        /// <inheritdoc />
        public int Compare(int x, int y)
        {
            FillValues(_values1, x);
            FillValues(_values2, y);

            for (int i = 0; i < _values1.Length; i++)
            {
                if (_greaterFunctions[i].Invoke(ref _values1[i], ref _values2[i]).AsBooleanUnsafe)
                {
                    return _greaterValues[i];
                }
                if (_lessFunctions[i].Invoke(ref _values1[i], ref _values2[i]))
                {
                    return _lessValues[i];
                }
            }
            return 0;
        }
    }

    private sealed class OrderColumnsIterator : IRowsIterator
    {
        private readonly OrderColumnsIndex _orderColumnsIndex;
        private int _currentRowIndex = -1;

        /// <inheritdoc />
        public Column[] Columns => _orderColumnsIndex.RowsFrameIterator.Columns;

        /// <inheritdoc />
        public Row Current => _orderColumnsIndex.RowsFrameIterator.Current;

        public OrderColumnsIterator(OrderColumnsIndex orderColumnsIndex)
        {
            _orderColumnsIndex = orderColumnsIndex;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_currentRowIndex < _orderColumnsIndex._rowsOrder.Length - 1)
            {
                _currentRowIndex++;
                _orderColumnsIndex.RowsFrameIterator.Seek(
                    _orderColumnsIndex._rowsOrder[_currentRowIndex], CursorSeekOrigin.Begin);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _currentRowIndex = -1;
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
            stringBuilder.AppendRowsIteratorsWithIndent("Sort", _orderColumnsIndex.RowsFrameIterator);
        }
    }

    public OrderColumnsIndex(
        ICursorRowsIterator rowsFrameIterator,
        IList<OrderDirection> directions,
        IList<int> columnIndexes)
    {
        RowsFrameIterator = rowsFrameIterator;
        _directions = directions.ToArray();
        _columnIndexes = columnIndexes.ToArray();
        _valueGetters = columnIndexes.Select(index => new FuncUnitRowsIteratorColumn(rowsFrameIterator, index))
            .ToArray();
    }

    /// <inheritdoc />
    public IRowsIterator GetOrderIterator() => new OrderColumnsIterator(this);

    /// <inheritdoc />
    public void Rebuild()
    {
        _rowsOrder = new int[RowsFrameIterator.TotalRows];
        for (var i = 0; i < RowsFrameIterator.TotalRows; i++)
        {
            _rowsOrder[i] = i;
        }

        var comparer = new RowsComparer(this);
        Array.Sort(_rowsOrder, comparer);
    }
}
