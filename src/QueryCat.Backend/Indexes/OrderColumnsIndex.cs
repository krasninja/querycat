using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Indexes;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Indexes;

/// <summary>
/// Order columns according to expression.
/// </summary>
internal sealed class OrderColumnsIndex : IOrderIndex
{
    private int[] _rowsOrder = [];
    private readonly OrderColumnData[] _orderColumnData;

    private ICursorRowsIterator RowsFrameIterator { get; }

    #region Column keys

    /// <summary>
    /// Sort keys of a single order column, stored column-major as native values.
    /// </summary>
    private abstract class ColumnKeys
    {
        private readonly bool[] _isNull;
        private readonly int _direction;
        private readonly int _leftNullResult;
        private bool _hasNulls;

        protected ColumnKeys(int count, OrderColumnData orderColumnData)
        {
            _isNull = new bool[count];
            _direction = orderColumnData.Direction == OrderDirection.Ascending ? 1 : -1;
            // Nulls placement does not depend on the direction.
            _leftNullResult = orderColumnData.NullOrder == NullOrder.NullsFirst ? -1 : 1;
        }

        public void Set(int rowIndex, in VariantValue value)
        {
            if (value.IsNull)
            {
                _isNull[rowIndex] = true;
                _hasNulls = true;
            }
            else
            {
                SetValue(rowIndex, in value);
            }
        }

        protected abstract void SetValue(int rowIndex, in VariantValue value);

        protected abstract int CompareValues(int x, int y);

        public int Compare(int x, int y)
        {
            if (_hasNulls)
            {
                var xIsNull = _isNull[x];
                var yIsNull = _isNull[y];
                if (xIsNull || yIsNull)
                {
                    if (xIsNull && yIsNull)
                    {
                        return 0;
                    }
                    return xIsNull ? _leftNullResult : -_leftNullResult;
                }
            }
            return CompareValues(x, y) * _direction;
        }
    }

    private sealed class Int64ColumnKeys(int count, OrderColumnData data, Func<VariantValue, long> getter)
        : ColumnKeys(count, data)
    {
        private readonly long[] _values = new long[count];

        /// <inheritdoc />
        protected override void SetValue(int rowIndex, in VariantValue value) => _values[rowIndex] = getter(value);

        /// <inheritdoc />
        protected override int CompareValues(int x, int y) => _values[x].CompareTo(_values[y]);
    }

    private sealed class FloatColumnKeys(int count, OrderColumnData data) : ColumnKeys(count, data)
    {
        private readonly double[] _values = new double[count];

        /// <inheritdoc />
        protected override void SetValue(int rowIndex, in VariantValue value) => _values[rowIndex] = value.AsFloatUnsafe;

        /// <inheritdoc />
        protected override int CompareValues(int x, int y)
        {
            // Do not use CompareTo to keep NaN semantic the same as for ">" and "<" operators.
            double left = _values[x], right = _values[y];
            return left > right ? 1 : left < right ? -1 : 0;
        }
    }

    private sealed class NumericColumnKeys(int count, OrderColumnData data) : ColumnKeys(count, data)
    {
        private readonly decimal[] _values = new decimal[count];

        /// <inheritdoc />
        protected override void SetValue(int rowIndex, in VariantValue value) => _values[rowIndex] = value.AsNumericUnsafe;

        /// <inheritdoc />
        protected override int CompareValues(int x, int y) => _values[x].CompareTo(_values[y]);
    }

    private sealed class StringColumnKeys(int count, OrderColumnData data) : ColumnKeys(count, data)
    {
        private readonly string[] _values = new string[count];

        /// <inheritdoc />
        protected override void SetValue(int rowIndex, in VariantValue value) => _values[rowIndex] = value.AsStringUnsafe;

        /// <inheritdoc />
        protected override int CompareValues(int x, int y) => string.CompareOrdinal(_values[x], _values[y]);
    }

    /// <summary>
    /// Fallback for types without native comparison: uses the generic greater/less operators.
    /// </summary>
    private sealed class VariantColumnKeys : ColumnKeys
    {
        private readonly VariantValue[] _values;
        private readonly VariantValue.BinaryFunction _greater;
        private readonly VariantValue.BinaryFunction _less;

        public VariantColumnKeys(int count, OrderColumnData data, DataType type) : base(count, data)
        {
            _values = new VariantValue[count];
            _greater = VariantValue.GetGreaterDelegate(type, type);
            _less = VariantValue.GetLessDelegate(type, type);
        }

        /// <inheritdoc />
        protected override void SetValue(int rowIndex, in VariantValue value) => _values[rowIndex] = value;

        /// <inheritdoc />
        protected override int CompareValues(int x, int y)
        {
            ref readonly var left = ref _values[x];
            ref readonly var right = ref _values[y];
            if (_greater.Invoke(in left, in right).AsBoolean)
            {
                return 1;
            }
            if (_less.Invoke(in left, in right).AsBoolean)
            {
                return -1;
            }
            return 0;
        }
    }

    private static ColumnKeys CreateColumnKeys(int count, OrderColumnData data, DataType type) => type switch
    {
        DataType.Integer => new Int64ColumnKeys(count, data, v => v.AsIntegerUnsafe),
        DataType.Boolean => new Int64ColumnKeys(count, data, v => v.AsBooleanUnsafe ? 1 : 0),
        DataType.Timestamp => new Int64ColumnKeys(count, data, v => v.AsTimestampUnsafe.Ticks),
        DataType.Interval => new Int64ColumnKeys(count, data, v => v.AsIntervalUnsafe.Ticks),
        DataType.Float => new FloatColumnKeys(count, data),
        DataType.Numeric => new NumericColumnKeys(count, data),
        DataType.String => new StringColumnKeys(count, data),
        _ => new VariantColumnKeys(count, data, type),
    };

    private sealed class RowsComparer(ColumnKeys[] columnKeys) : IComparer<int>
    {
        /// <inheritdoc />
        public int Compare(int x, int y)
        {
            foreach (var keys in columnKeys)
            {
                var result = keys.Compare(x, y);
                if (result != 0)
                {
                    return result;
                }
            }
            // Keep the original order for equal rows: makes the sort stable and the comparer consistent.
            return x.CompareTo(y);
        }
    }

    #endregion

    private sealed class OrderColumnsIterator : ICursorRowsIterator
    {
        private readonly OrderColumnsIndex _orderColumnsIndex;
        private long _currentRowIndex = -1;

        /// <inheritdoc />
        public Column[] Columns => _orderColumnsIndex.RowsFrameIterator.Columns;

        /// <inheritdoc />
        public Row Current => _orderColumnsIndex.RowsFrameIterator.Current;

        /// <inheritdoc />
        public long Position => _orderColumnsIndex._rowsOrder[_currentRowIndex];

        /// <inheritdoc />
        public long TotalRows => _orderColumnsIndex.RowsFrameIterator.TotalRows;

        public OrderColumnsIterator(OrderColumnsIndex orderColumnsIndex)
        {
            _orderColumnsIndex = orderColumnsIndex;
        }

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (_currentRowIndex < _orderColumnsIndex._rowsOrder.Length - 1)
            {
                _currentRowIndex++;
                _orderColumnsIndex.RowsFrameIterator.Seek(
                    _orderColumnsIndex._rowsOrder[_currentRowIndex], CursorSeekOrigin.Begin);
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            _currentRowIndex = -1;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
            stringBuilder.AppendRowsIteratorsWithIndent("Sort", _orderColumnsIndex.RowsFrameIterator);
        }

        /// <inheritdoc />
        public void Seek(long offset, CursorSeekOrigin origin)
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

    public OrderColumnsIndex(ICursorRowsIterator rowsFrameIterator, OrderColumnData[] orderColumnData)
    {
        RowsFrameIterator = rowsFrameIterator;
        _orderColumnData = orderColumnData;
    }

    /// <inheritdoc />
    public ICursorRowsIterator GetOrderIterator() => new OrderColumnsIterator(this);

    /// <inheritdoc />
    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        var totalRows = (int)RowsFrameIterator.TotalRows;
        var rowsOrder = new int[totalRows];
        for (var i = 0; i < totalRows; i++)
        {
            rowsOrder[i] = i;
        }

        if (totalRows > 1 && _orderColumnData.Length > 0)
        {
            var columnKeys = await LoadColumnKeysAsync(totalRows, cancellationToken);
            Array.Sort(rowsOrder, new RowsComparer(columnKeys));
        }
        _rowsOrder = rowsOrder;
    }

    private async ValueTask<ColumnKeys[]> LoadColumnKeysAsync(int totalRows, CancellationToken cancellationToken)
    {
        var columns = RowsFrameIterator.Columns;
        var columnKeys = new ColumnKeys[_orderColumnData.Length];
        for (var i = 0; i < _orderColumnData.Length; i++)
        {
            var data = _orderColumnData[i];
            columnKeys[i] = CreateColumnKeys(totalRows, data, columns[data.Index].DataType);
        }

        await RowsFrameIterator.ResetAsync(cancellationToken);
        var rowIndex = 0;
        while (rowIndex < totalRows && await RowsFrameIterator.MoveNextAsync(cancellationToken))
        {
            var row = RowsFrameIterator.Current;
            for (var i = 0; i < columnKeys.Length; i++)
            {
                columnKeys[i].Set(rowIndex, row[_orderColumnData[i].Index]);
            }
            rowIndex++;
        }
        return columnKeys;
    }
}
