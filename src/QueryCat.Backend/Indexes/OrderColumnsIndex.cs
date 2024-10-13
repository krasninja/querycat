using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Indexes;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Indexes;

/// <summary>
/// Order columns according to expression.
/// </summary>
internal sealed class OrderColumnsIndex : IOrderIndex
{
    private readonly IExecutionThread _thread;
    private int[] _rowsOrder = [];
    private readonly OrderDirection[] _directions;
    private readonly IFuncUnit[] _valueGetters;
    private readonly NullOrder[] _nullOrders;
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
            _greaterFunctions = columnsTypes
                .Select((t, i) => GetGreaterDelegate(t, _orderColumnsIndex._nullOrders[i], _greaterValues[i]))
                .ToArray();
            _lessFunctions = columnsTypes
                .Select((t, i) => GetLessDelegate(t, _orderColumnsIndex._nullOrders[i], _lessValues[i]))
                .ToArray();
        }

        private static VariantValue.BinaryFunction GetGreaterDelegate(
            DataType type,
            NullOrder nullOrder,
            int greater)
        {
            var greaterValue = new VariantValue(greater == -1);
            var lessValue = new VariantValue(!greaterValue.AsBooleanUnsafe);
            var func = VariantValue.GetGreaterDelegate(type, type);
            return (in VariantValue left, in VariantValue right) =>
            {
                if (left.IsNull)
                {
                    return nullOrder == NullOrder.NullsFirst ? greaterValue : lessValue;
                }
                else if (right.IsNull)
                {
                    return nullOrder == NullOrder.NullsFirst ? lessValue : greaterValue;
                }
                return func.Invoke(in left, in right);
            };
        }

        private static VariantValue.BinaryFunction GetLessDelegate(
            DataType type,
            NullOrder nullOrder,
            int less)
        {
            var lessValue = new VariantValue(less == -1);
            var greaterValue = new VariantValue(!lessValue.AsBooleanUnsafe);
            var func = VariantValue.GetLessDelegate(type, type);
            return (in VariantValue left, in VariantValue right) =>
            {
                if (left.IsNull)
                {
                    return nullOrder == NullOrder.NullsFirst ? lessValue : greaterValue;
                }
                else if (right.IsNull)
                {
                    return nullOrder == NullOrder.NullsFirst ? greaterValue : lessValue;
                }
                return func.Invoke(in left, in right);
            };
        }

        private void FillValues(VariantValue[] values, int rowIndex)
        {
            _orderColumnsIndex.RowsFrameIterator.Seek(rowIndex, CursorSeekOrigin.Begin);
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = _orderColumnsIndex._valueGetters[i].Invoke(_orderColumnsIndex._thread);
            }
        }

        /// <inheritdoc />
        public int Compare(int x, int y)
        {
            FillValues(_values1, x);
            FillValues(_values2, y);

            for (var i = 0; i < _values1.Length; i++)
            {
                if (_greaterFunctions[i].Invoke(in _values1[i], in _values2[i]).AsBooleanUnsafe)
                {
                    return _greaterValues[i];
                }
                if (_lessFunctions[i].Invoke(in _values1[i], in _values2[i]))
                {
                    return _lessValues[i];
                }
            }
            return 0;
        }
    }

    private sealed class OrderColumnsIterator : ICursorRowsIterator
    {
        private readonly OrderColumnsIndex _orderColumnsIndex;
        private int _currentRowIndex = -1;

        /// <inheritdoc />
        public Column[] Columns => _orderColumnsIndex.RowsFrameIterator.Columns;

        /// <inheritdoc />
        public Row Current => _orderColumnsIndex.RowsFrameIterator.Current;

        /// <inheritdoc />
        public int Position => _orderColumnsIndex._rowsOrder[_currentRowIndex];

        /// <inheritdoc />
        public int TotalRows => _orderColumnsIndex.RowsFrameIterator.TotalRows;

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

        /// <inheritdoc />
        public void Seek(int offset, CursorSeekOrigin origin)
        {
            if (origin == CursorSeekOrigin.Begin)
            {
                _currentRowIndex = offset;
            }
            else if (origin == CursorSeekOrigin.Current)
            {
                _currentRowIndex += offset;
            }
            else if (origin == CursorSeekOrigin.End)
            {
                _currentRowIndex = TotalRows - offset;
            }
        }
    }

    public OrderColumnsIndex(
        IExecutionThread thread,
        ICursorRowsIterator rowsFrameIterator,
        OrderColumnData[] orderColumnData)
    {
        _thread = thread;
        RowsFrameIterator = rowsFrameIterator;
        _directions = orderColumnData.Select(d => d.Direction).ToArray();
        _nullOrders = orderColumnData.Select(d => d.NullOrder).ToArray();
        _columnIndexes = orderColumnData.Select(d => d.Index).ToArray();
        _valueGetters = _columnIndexes.Select(index => new FuncUnitRowsIteratorColumn(rowsFrameIterator, index))
            .ToArray();
    }

    /// <inheritdoc />
    public ICursorRowsIterator GetOrderIterator() => new OrderColumnsIterator(this);

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
