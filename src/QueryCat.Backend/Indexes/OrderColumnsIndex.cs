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
    private readonly FuncUnit[] _valueGetters;

    private ICursorRowsIterator RowsFrameIterator { get; }

    private class RowsComparer : IComparer<int>
    {
        private readonly OrderColumnsIndex _orderColumnsIndex;

        private readonly VariantValue[] _values1;
        private readonly VariantValue[] _values2;

        private readonly int[] _greaterValues;
        private readonly int[] _lessValues;

        public RowsComparer(OrderColumnsIndex orderColumnsIndex)
        {
            _orderColumnsIndex = orderColumnsIndex;
            FuncUnit.SetIterator(_orderColumnsIndex._valueGetters, _orderColumnsIndex.RowsFrameIterator);

            _values1 = new VariantValue[_orderColumnsIndex._valueGetters.Length];
            _values2 = new VariantValue[_orderColumnsIndex._valueGetters.Length];

            _greaterValues = _orderColumnsIndex._directions
                .Select(d => d == OrderDirection.Ascending ? 1 : -1).ToArray();
            _lessValues = _orderColumnsIndex._directions
                .Select(d => d == OrderDirection.Ascending ? -1 : 1).ToArray();
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
                if (VariantValue.Greater(ref _values1[i], ref _values2[i], out _))
                {
                    return _greaterValues[i];
                }
                if (VariantValue.Less(ref _values1[i], ref _values2[i], out _))
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
        public Row Current
        {
            get
            {
                _orderColumnsIndex.RowsFrameIterator.Seek(_orderColumnsIndex._rowsOrder[_currentRowIndex], CursorSeekOrigin.Begin);
                return _orderColumnsIndex.RowsFrameIterator.Current;
            }
        }

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
        IEnumerable<OrderDirection> directions,
        IEnumerable<FuncUnit> valueGetters)
    {
        RowsFrameIterator = rowsFrameIterator;
        _directions = directions.ToArray();
        _valueGetters = valueGetters.ToArray();
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
