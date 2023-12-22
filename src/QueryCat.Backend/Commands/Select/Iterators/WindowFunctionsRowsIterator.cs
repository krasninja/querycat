using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Indexes;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal sealed class WindowFunctionsRowsIterator : IRowsIterator
{
    private record PartitionInstance(
        RowsFrame RowsFrame,
        ICursorRowsIterator RowsIterator);

    private record struct RowIdData(
        PartitionInstance PartitionInstance,
        long RowIdInPartition);

    private sealed class WindowInfo : IWindowInfo
    {
        public RowsFrame RowsFrame { get; set; } = RowsFrame.Empty;

        public long CurrentRowPosition { get; set; }

        /// <inheritdoc />
        public long GetTotalRows() => RowsFrame.TotalRows;

        /// <inheritdoc />
        public long GetCurrentRowPosition() => CurrentRowPosition;
    }

    private sealed class PartitionInfo
    {
        public int OriginalColumnIndex { get; }

        public Func<VariantValueArray> PartitionFormatter { get; }

        public WindowFunctionInfo WindowFunctionInfo { get; }

        /*
         * RowsFrame contains order columns + row id (from _rowId). It is needed if we have ORDER BY clause.
         * Example: SELECT avg(balance) over (order by dep, balance) FROM ...
         * RowsFrame columns: balance | dep, balance | index.
         *
         * The order information (direction, nulls) we can get from WindowFunctionInfo.Orders .
         */
        public Dictionary<VariantValueArray, PartitionInstance> PartitionRowsIds { get; } = new();

        /// <summary>
        /// Related to KeysRowsIds.Value columns.
        /// </summary>
        public Column[] Columns { get; }

        /// <summary>
        /// Related to KeysRowsIds.Value row.
        /// </summary>
        private readonly Row _row;

        private readonly WindowInfo _windowInfo = new();

        /// <summary>
        /// Global RowId => Partition and OrderIterator.
        /// </summary>
        public List<RowIdData> RowIdToPartition { get; } = new();

        public bool HasOrderData => WindowFunctionInfo.Orders.Length > 0;

        public PartitionInfo(
            int originalColumnIndex,
            Func<VariantValueArray> partitionFormatter,
            WindowFunctionInfo windowFunctionInfo)
        {
            WindowFunctionInfo = windowFunctionInfo;
            OriginalColumnIndex = originalColumnIndex;
            PartitionFormatter = partitionFormatter;
            Columns = windowFunctionInfo
                // Aggregates.
                .AggregateValues.Select((o, i) => new Column("__a" + i, o.OutputType))
                // Order.
                .Concat(
                    windowFunctionInfo.OrderFunctions.Select(
                        (o, i) => new Column("__o" + i, windowFunctionInfo.OrderFunctions[i].OutputType)))
                .ToArray();

            // Some aggregate functions has no input args (row_number, count). Since RowFrame cannot be without
            // rows - just make the fake one.
            if (Columns.Length == 0)
            {
                Columns = RowsFrame.Empty.Columns;
            }
            _row = new Row(Columns);
        }

        public PartitionInstance Add(VariantValueArray partitionKey, IList<IIndex> indexes)
        {
            if (!PartitionRowsIds.TryGetValue(partitionKey, out var partitionData))
            {
                var subRowsFrame = new RowsFrame(Columns);
                ICursorRowsIterator subRowsFrameIterator;
                var offset = WindowFunctionInfo.AggregateValues.Length;
                if (WindowFunctionInfo.Orders.Length > 0)
                {
                    var index = new OrderColumnsIndex(
                        subRowsFrame.GetIterator(),
                        WindowFunctionInfo.Orders
                            .Select(o => new OrderColumnData(o.Index + offset, o.Direction, o.NullOrder))
                            .ToArray());
                    indexes.Add(index);
                    // The index iterator implementation is lazy, so it will not be called right away.
                    subRowsFrameIterator = index.GetOrderIterator();
                }
                else
                {
                    subRowsFrameIterator = subRowsFrame.GetIterator();
                }
                partitionData = new PartitionInstance(subRowsFrame, subRowsFrameIterator);
                PartitionRowsIds.Add(partitionKey, partitionData);
            }

            // Format result rows frame. Format: agg_arg1, agg_arg2, order_col1, order_col2, rowId.
            var nextIndex = 0;
            // Aggregates.
            for (var i = 0; i < WindowFunctionInfo.AggregateValues.Length; i++)
            {
                _row[nextIndex++] = WindowFunctionInfo.AggregateValues[i].Invoke();
            }
            // Order.
            for (var i = 0; i < WindowFunctionInfo.OrderFunctions.Length; i++)
            {
                _row[nextIndex++] = WindowFunctionInfo.OrderFunctions[i].Invoke();
            }
            var rowIndex = partitionData.RowsFrame.AddRow(_row);
            RowIdToPartition.Add(new RowIdData(partitionData, rowIndex));

            return partitionData;
        }

        public void FillAggregateFunctionArguments(FunctionCallInfo functionCallInfo, RowIdData rowIdData)
        {
            functionCallInfo.Reset();
            _windowInfo.RowsFrame = rowIdData.PartitionInstance.RowsFrame;
            _windowInfo.CurrentRowPosition = rowIdData.RowIdInPartition;
            functionCallInfo.WindowInfo = _windowInfo;
            for (var aggregateIndex = 0; aggregateIndex < WindowFunctionInfo.AggregateValues.Length; aggregateIndex++)
            {
                functionCallInfo.Push(rowIdData.PartitionInstance.RowsIterator.Current[aggregateIndex]);
            }
        }
    }

    private readonly IRowsIterator _rowsIterator;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private readonly PartitionInfo[] _partitions;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    public WindowFunctionsRowsIterator(
        IRowsIterator rowsIterator,
        IEnumerable<WindowFunctionInfo> windowFunctionInfos)
    {
        _rowsIterator = rowsIterator;

        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();

        // Prepare initial data for window partitions.
        _partitions = windowFunctionInfos
            .Select(info => new PartitionInfo(
                originalColumnIndex: info.ColumnIndex,
                partitionFormatter: () => new VariantValueArray(info.PartitionFormatters),
                windowFunctionInfo: info
            ))
            .ToArray();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            FillRowsAndPrepareWindowData();
            FillWindowColumns();
            _isInitialized = true;
        }

        return _rowsFrameIterator.MoveNext();
    }

    private void FillRowsAndPrepareWindowData()
    {
        var indexes = new List<IIndex>();

        // Prefetch all values.
        while (_rowsIterator.MoveNext())
        {
            for (var partIndex = 0; partIndex < _partitions.Length; partIndex++)
            {
                var partition = _partitions[partIndex];

                // Find partition key. Add it into KeysRowsIds.
                var key = partition.PartitionFormatter.Invoke();
                partition.Add(key, indexes);
            }

            // Add the row into rows frame.
            _rowsFrame.AddRow(_rowsIterator.Current);
        }

        // Rebuild indexes.
        foreach (var index in indexes)
        {
            index.Rebuild();
        }
    }

    private void FillWindowColumns()
    {
        var iterator = _rowsFrame.GetIterator();
        while (iterator.MoveNext())
        {
            for (var partIndex = 0; partIndex < _partitions.Length; partIndex++)
            {
                var partition = _partitions[partIndex];
                var aggregateValue = ProcessPartition(iterator, partition);

                _rowsFrame.UpdateValue(iterator.Position, partition.OriginalColumnIndex, aggregateValue);
            }
        }
    }

    private VariantValue ProcessPartition(ICursorRowsIterator iterator, PartitionInfo partitionInfo)
    {
        var rowIdData = partitionInfo.RowIdToPartition[iterator.Position];

        var aggregateTarget = partitionInfo.WindowFunctionInfo.AggregateTarget;
        var aggregateState = aggregateTarget.AggregateFunction.GetInitialState(aggregateTarget.ReturnType);

        // Order.
        var partitionRow = rowIdData.PartitionInstance.RowsFrame.GetRow((int)rowIdData.RowIdInPartition);

        // Calculate aggregate values by certain window boundaries.
        var rowsIterator = rowIdData.PartitionInstance.RowsIterator;
        rowsIterator.Reset();
        while (rowsIterator.MoveNext())
        {
            // Upper boundary.

            // Full argument function arguments and invoke.
            partitionInfo.FillAggregateFunctionArguments(aggregateTarget.FunctionCallInfo, rowIdData);
            aggregateTarget.AggregateFunction.Invoke(aggregateState, aggregateTarget.FunctionCallInfo);

            // Lower boundary.
            if (partitionInfo.HasOrderData
                && RowsEquals(partitionRow, rowsIterator.Current, partitionInfo.WindowFunctionInfo.Orders.Length))
            {
                break;
            }
        }

        return aggregateTarget.AggregateFunction.GetResult(aggregateState);
    }

    private static bool RowsEquals(Row row1, Row row2, int limit)
    {
        for (var i = 0; i < row1.Columns.Length && i < limit; i++)
        {
            if (row1[i] != row2[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isInitialized = false;
        _rowsFrame.Clear();
        _rowsFrameIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Window", _rowsIterator);
    }
}
