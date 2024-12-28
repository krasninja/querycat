using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
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

    private readonly record struct RowIdData(
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
        private readonly IExecutionThread _thread;

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
            IExecutionThread thread,
            int originalColumnIndex,
            Func<VariantValueArray> partitionFormatter,
            WindowFunctionInfo windowFunctionInfo)
        {
            _thread = thread;
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
                        _thread,
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
            foreach (var aggregateValue in WindowFunctionInfo.AggregateValues)
            {
                _row[nextIndex++] = aggregateValue.Invoke(_thread);
            }
            // Order.
            foreach (var orderFunction in WindowFunctionInfo.OrderFunctions)
            {
                _row[nextIndex++] = orderFunction.Invoke(_thread);
            }
            var rowIndex = partitionData.RowsFrame.AddRow(_row);
            RowIdToPartition.Add(new RowIdData(partitionData, rowIndex));

            return partitionData;
        }

        public void FillAggregateFunctionArguments(RowIdData rowIdData)
        {
            _windowInfo.RowsFrame = rowIdData.PartitionInstance.RowsFrame;
            _windowInfo.CurrentRowPosition = rowIdData.RowIdInPartition;
            for (var aggregateIndex = 0; aggregateIndex < WindowFunctionInfo.AggregateValues.Length; aggregateIndex++)
            {
                _thread.Stack.Push(rowIdData.PartitionInstance.RowsIterator.Current[aggregateIndex]);
            }
            _thread.Stack.Push(VariantValue.CreateFromObject(_windowInfo));
        }
    }

    private readonly IExecutionThread _thread;
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
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IEnumerable<WindowFunctionInfo> windowFunctionInfos)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;

        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();

        // Prepare initial data for window partitions.
        _partitions = windowFunctionInfos
            .Select(info => new PartitionInfo(
                thread,
                originalColumnIndex: info.ColumnIndex,
                partitionFormatter: () => new VariantValueArray(info.PartitionFormatters.Select(f => f.Invoke(thread))),
                windowFunctionInfo: info
            ))
            .ToArray();
    }

    private async ValueTask FillRowsAndPrepareWindowData(CancellationToken cancellationToken)
    {
        var indexes = new List<IIndex>();

        // Prefetch all values.
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            foreach (var partition in _partitions)
            {
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

    private async ValueTask FillWindowColumnsAsync(CancellationToken cancellationToken)
    {
        var iterator = _rowsFrame.GetIterator();
        while (await iterator.MoveNextAsync(cancellationToken))
        {
            foreach (var partition in _partitions)
            {
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
            using var frame = _thread.Stack.CreateFrame();
            partitionInfo.FillAggregateFunctionArguments(rowIdData);
            aggregateTarget.AggregateFunction.Invoke(aggregateState, _thread);

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
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await FillRowsAndPrepareWindowData(cancellationToken);
            await FillWindowColumnsAsync(cancellationToken);
            _isInitialized = true;
        }

        return await _rowsFrameIterator.MoveNextAsync(cancellationToken);
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
