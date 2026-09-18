using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal sealed class GroupRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly int _id = IdGenerator.GetNext();

    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private bool _isInitialized;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private readonly int _aggregateColumnsOffset;
    private readonly IFuncUnit[] _keys;
    private readonly SelectCommandContext _context;
    private readonly AggregateTarget[] _targets;
    private readonly bool _isSingleGroup;
    private Dictionary<VariantValueArray, GroupKeyEntry>? _keysRowIndexesMap;
    private readonly IFuncUnitArguments?[] _targetsArguments;

    internal static IFuncUnit[] NoGroupsKeyFactory { get; } =
    {
        new FuncUnitStatic(VariantValue.OneIntegerValue)
    };

    /*
     * For aggregate queries we break pipeline execution and have to prepare new rows frame.
     * We also prepare new columns. For example:
     *
     * Table: id, first, last, balance
     * SELECT sum(balance) FROM tbl GROUP BY first HAVING count(1) > 2;
     *
     * Final aggregate rows frame columns:
     * id, first, last, balance, sum(balance), count(1)
     *
     * 0-3 - the columns copy from input table
     * 4-5 - calculated aggregates
     * aggregateColumnsOffset = 4
     */

    private readonly struct GroupKeyEntry
    {
        public VariantValueArray[] AggregateStates { get; }

        public int RowIndex { get; }

        public GroupKeyEntry(VariantValueArray[] aggregateStates, int rowIndex)
        {
            AggregateStates = aggregateStates;
            RowIndex = rowIndex;
        }

        /// <inheritdoc />
        public override string ToString()
            => $"{RowIndex}: {string.Join(" | ", AggregateStates.Select(s => s.ToString()))}";
    }

    /// <inheritdoc />
    public Column[] Columns => _rowsFrameIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    internal RowsFrame RowsFrame => _rowsFrame;

    private bool IsSingleGroup => _keys == NoGroupsKeyFactory || _isSingleGroup;

    public GroupRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IFuncUnit[] keys,
        SelectCommandContext context,
        AggregateTarget[] targets,
        bool isSingleGroup = false)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        _keys = keys;
        _context = context;
        _targets = targets;
        _isSingleGroup = isSingleGroup;

        var columns = GetAggregateColumns(rowsIterator, targets);
        _aggregateColumnsOffset = rowsIterator.Columns.Length;
        _rowsFrame = new RowsFrame(columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();
        _targetsArguments = new IFuncUnitArguments?[targets.Length];
        for (var i = 0; i < targets.Length; i++)
        {
            _targetsArguments[i] = targets[i].ValueGenerator as IFuncUnitArguments;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await FillRowsAsync(cancellationToken);
            _isInitialized = true;
        }

        return await _rowsFrameIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isInitialized = false;
        _rowsFrame.Clear();
        await _rowsIterator.ResetAsync(cancellationToken);
        await _rowsFrameIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent(
                $"Group By (keys={_keys.Length}, targets={_targets.Length}, id={_id})", _rowsIterator)
            .AppendSubQueriesWithIndent(_keys);
    }

    private static VariantValueArray[] TargetsToInitialStates(AggregateTarget[] targets)
    {
        var arr = new VariantValueArray[targets.Length];
        for (var i = 0; i < targets.Length; i++)
        {
            arr[i] = new VariantValueArray(targets[i].AggregateFunction.GetInitialState(targets[i].ReturnType));
        }
        return arr;
    }

    private async ValueTask FillRowsAsync(CancellationToken cancellationToken)
    {
        _keysRowIndexesMap ??= new Dictionary<VariantValueArray, GroupKeyEntry>(capacity: IsSingleGroup ? 1 : 64);
        var keysRowIndexesMap = _keysRowIndexesMap;
        keysRowIndexesMap.Clear();

        var probeKey = new VariantValue[_keys.Length];

        // Fill keysRowIndexesMap.
        var row = new Row(_rowsFrame);
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            // Format key and fill aggregate values.
            for (var i = 0; i < _keys.Length; i++)
            {
                probeKey[i] = await _keys[i].InvokeAsync(_thread, cancellationToken);
            }
            var probeKeyArray = new VariantValueArray(probeKey);
            if (!keysRowIndexesMap.TryGetValue(probeKeyArray, out GroupKeyEntry groupKey))
            {
                _rowsIterator.Current.Copy(row);
                VariantValueArray[] initialStates = TargetsToInitialStates(_targets);
                groupKey = new GroupKeyEntry(initialStates, _rowsFrame.AddRow(row));
                keysRowIndexesMap.Add(probeKeyArray, groupKey);
                probeKey = new VariantValueArray(size: _keys.Length);
            }

            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                using var frame = _thread.Stack.CreateFrame();
                await FillAggregateTargetStackValuesAsync(target, _targetsArguments[i], cancellationToken);
                target.AggregateFunction.Invoke(groupKey.AggregateStates[i], _thread);
            }
        }

        // Fill rows frame.
        if (_targets.Length > 0)
        {
            if (keysRowIndexesMap.Count > 0)
            {
                var valuesArray = new VariantValue[_targets.Length];
                foreach (var mapValue in keysRowIndexesMap.Values)
                {
                    for (var i = 0; i < _targets.Length; i++)
                    {
                        valuesArray[i] = _targets[i].AggregateFunction.GetResult(mapValue.AggregateStates[i]);
                    }
                    _rowsFrame.UpdateValues(mapValue.RowIndex, _aggregateColumnsOffset, valuesArray);
                }
            }
            else if (IsSingleGroup)
            {
                // If no data at all - we produce default result.
                var defaultValuesRow = new Row(_rowsFrame);
                for (var i = 0; i < _targets.Length; i++)
                {
                    var target = _targets[i];
                    defaultValuesRow[_aggregateColumnsOffset + i] = target.AggregateFunction.GetResult(
                        target.AggregateFunction.GetInitialState(target.ReturnType));
                }
                _rowsFrame.AddRow(defaultValuesRow);
            }
        }

        keysRowIndexesMap.Clear();
    }

    private async ValueTask FillAggregateTargetStackValuesAsync(
        AggregateTarget target,
        IFuncUnitArguments? arguments,
        CancellationToken cancellationToken)
    {
        if (arguments != null)
        {
            var units = arguments.ArgumentsUnits;
            for (var i = 0; i < units.Length; i++)
            {
                _thread.Stack.Push(await units[i].InvokeAsync(_thread, cancellationToken));
            }
        }
        else
        {
            // We need this call to fill FunctionCallInfo.
            await target.ValueGenerator.InvokeAsync(_thread, cancellationToken);
        }
    }

    private Column[] GetAggregateColumns(IRowsIterator rows, AggregateTarget[] targets)
    {
        var columns = new Column[rows.Columns.Length + targets.Length];
        Array.Copy(rows.Columns, columns, rows.Columns.Length);
        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var columnName = !string.IsNullOrEmpty(target.Name) ? target.Name : $"__a-{target.Node.Id}";
            var column = new Column(columnName, target.ReturnType);
            columns[rows.Columns.Length + i] = column;
            _context.ColumnsInfoContainer.GetByColumnOrAdd(column).IsAggregateKey = true;
        }
        return columns;
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
